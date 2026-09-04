<?php

declare(strict_types=1);

namespace plugins\payment\tokenpay;

use app\common\BasePayment;
use app\common\PaymentContext;

class TokenpayPlugin extends BasePayment
{
    public function submit(PaymentContext $ctx): array
    {
        try {
            $data = [
                'OutOrderId' => (string) $ctx->order['trade_no'],
                'OrderUserKey' => (string) $ctx->order['trade_no'],
                'ActualAmount' => $this->amount($ctx->order),
                'Currency' => $this->currency($ctx),
                'NotifyUrl' => config_get('localurl') . 'pay/notify/' . $ctx->order['trade_no'] . '/',
                'RedirectUrl' => request()->siteurl . 'pay/return/' . $ctx->order['trade_no'] . '/',
            ];
            $data['Signature'] = $this->sign($data);
            $result = $this->postJson('/CreateOrder', $data);
            if (empty($result['success'])) {
                return ['type' => 'error', 'msg' => (string) ($result['message'] ?? 'TokenPay 创建订单失败')];
            }
            $url = (string) ($result['data'] ?? '');
            if ($url === '') {
                return ['type' => 'error', 'msg' => 'TokenPay 未返回支付链接'];
            }
            return ['type' => 'jump', 'url' => $url];
        } catch (\Throwable $e) {
            return ['type' => 'error', 'msg' => $e->getMessage()];
        }
    }

    public function notify(PaymentContext $ctx): array
    {
        $data = $this->callbackData();
        if (!$data || !$this->verify($data) || (string) ($data['OutOrderId'] ?? '') !== (string) $ctx->order['trade_no']) {
            return ['type' => 'html', 'data' => 'fail'];
        }
        try {
            if (!$this->isPaymentConfirmed($data)) return $this->acknowledge(false);
            $this->processNotify($ctx->order, (string) $data['Id'], (string) ($data['FromAddress'] ?? ''), (string) ($data['BlockTransactionId'] ?? ''));
        } catch (\Throwable) {
            return ['type' => 'html', 'data' => 'fail'];
        }
        return $this->acknowledge(true);
    }

    //支付返回页面
    public function return(PaymentContext $ctx): array
    {
        return ['type' => 'page', 'page' => 'return'];
    }

    private function postJson(string $path, array $data): array
    {
        return $this->httpJson('POST', $path, json_encode($data, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES | JSON_THROW_ON_ERROR));
    }

    private function callbackData(): array
    {
        $data = request()->post();
        return is_array($data) ? $data : [];
    }

    private function queryOrder(string $id): array
    {
        $params = ['Id' => $id];
        $params['Signature'] = $this->sign($params);
        $result = $this->httpJson('GET', '/Query?' . http_build_query($params, '', '&', PHP_QUERY_RFC3986));
        if (!is_array($result) || empty($result['success']) || !is_array($result['data'] ?? null)) {
            throw new \RuntimeException('TokenPay 查单失败');
        }
        return $result['data'];
    }

    private function isPaymentConfirmed(array $callback): bool
    {
        $id = (string) ($callback['Id'] ?? '');
        if ($id === '' || (int) ($callback['Status'] ?? 0) !== 1) return false;
        $query = array_change_key_case($this->queryOrder($id), CASE_LOWER);
        return (int) ($query['status'] ?? 0) === 1 && $this->sameOrderData($callback, $query);
    }

    private function sameOrderData(array $callback, array $query): bool
    {
        foreach ($callback as $key => $value) {
            if (strcasecmp($key, 'Signature') === 0) continue;
            $queryKey = strtolower($key);
            if (!array_key_exists($queryKey, $query) || (string) $value !== (string) $query[$queryKey]) {
                return false;
            }
        }
        return true;
    }

    private function httpJson(string $method, string $path, ?string $body = null): array
    {
        $url = $this->apiUrl() . $path;
        $curl = curl_init($url);
        if ($curl === false) throw new \RuntimeException('TokenPay 接口初始化失败');
        curl_setopt_array($curl, [
            CURLOPT_CUSTOMREQUEST => $method,
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_CONNECTTIMEOUT => 10,
            CURLOPT_TIMEOUT => 20,
            CURLOPT_HTTPHEADER => ['Accept: application/json', 'Content-Type: application/json'],
            CURLOPT_SSL_VERIFYPEER => true,
            CURLOPT_SSL_VERIFYHOST => 2,
        ]);
        if ($body !== null) curl_setopt($curl, CURLOPT_POSTFIELDS, $body);
        $response = curl_exec($curl);
        $status = (int) curl_getinfo($curl, CURLINFO_RESPONSE_CODE);
        curl_close($curl);
        if ($response === false || $status < 200 || $status >= 300) {
            throw new \RuntimeException('TokenPay 接口请求失败');
        }
        $result = json_decode($response, true);
        if (!is_array($result)) throw new \RuntimeException('TokenPay 返回数据格式错误');
        return $result;
    }

    private function acknowledge(bool $success): array
    {
        return ['type' => 'html', 'data' => $success ? 'ok' : 'fail'];
    }

    private function verify(array $data): bool
    {
        $signature = (string) ($data['Signature'] ?? '');
        return $signature !== '' && hash_equals($this->sign($data), strtolower($signature));
    }

    private function sign(array $data): string
    {
        unset($data['Signature']);
        $data = array_filter($data, static fn ($value): bool => $value !== null && $value !== '');
        ksort($data, SORT_STRING);
        $canonical = implode('&', array_map(static fn ($key, $value): string => $key . '=' . (is_bool($value) ? ($value ? 'true' : 'false') : (string) $value), array_keys($data), $data));
        $token = $this->config('api_token');
        return ($this->channel['signature_algorithm'] ?? '0') === '1'
            ? hash_hmac('sha256', $canonical, $token)
            : md5($canonical . $token);
    }

    private function config(string $key, string $default = ''): string
    {
        $value = $this->channel[$key] ?? $default;
        if (!is_string($value) || trim($value) === '') {
            if ($default !== '') return $default;
            throw new \RuntimeException('TokenPay 配置项 ' . $key . ' 不能为空');
        }
        return trim($value);
    }

    private function apiUrl(): string
    {
        $url = rtrim($this->config('api_url'), '/');
        $parts = parse_url($url);
        if (!is_array($parts) || !isset($parts['host']) || !in_array($parts['scheme'] ?? '', ['http', 'https'], true)) {
            throw new \RuntimeException('TokenPay 地址格式错误');
        }
        return $url;
    }

    private function amount(array $order): string
    {
        $amount = $order['realmoney'] ?? $order['money'] ?? null;
        if (!is_numeric($amount) || (float) $amount <= 0) {
            throw new \RuntimeException('订单金额错误');
        }
        return number_format((float) $amount, 2, '.', '');
    }

    private function currency(PaymentContext $ctx): string
    {
        $currency = $ctx->order['typename'] ?? '';
        if (!is_string($currency) || $currency === '') {
            throw new \RuntimeException('未获取到当前支付类型');
        }
        return $currency;
    }
}
