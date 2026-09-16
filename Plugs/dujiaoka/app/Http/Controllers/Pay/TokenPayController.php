<?php

namespace App\Http\Controllers\Pay;

use App\Exceptions\RuleValidationException;
use App\Http\Controllers\PayController;
use GuzzleHttp\Client;
use GuzzleHttp\Exception\GuzzleException;
use Illuminate\Http\Request;

class TokenPayController extends PayController
{
    /**
     * 签名算法
     *
     * 可选：
     * MD5 兼容旧版TokenPay
     * HmacSha256 推荐
     */
    private const SIGNATURE_ALGORITHM = 'HmacSha256';

    private $url_notify = '';
    private $url_return = '';

    public function __construct()
    {
        $this->url_notify = url('/pay/tokenpay/notify_url');
        $this->url_return = url('/pay/tokenpay/return_url');
    }

    public function gateway(string $payway, string $orderSN)
    {
        try {
            $this->loadGateWay($orderSN, $payway);

            $parameter = [
                'ActualAmount' => (float)$this->order->actual_price,
                'OutOrderId' => $this->order->order_sn,
                'OrderUserKey' => $this->order->email,
                'Currency' => $this->payGateway->merchant_id,
                'RedirectUrl' => route('tokenpay-return', ['order_id' => $this->order->order_sn]),
                'NotifyUrl' => url($this->payGateway->pay_handleroute . '/notify_url'),
            ];

            $parameter['Signature'] = $this->generateSign($parameter, $this->payGateway->merchant_key);

            $client = new Client([
                'headers' => ['Content-Type' => 'application/json']
            ]);

            $response = $client->post($this->payGateway->merchant_pem . '/CreateOrder', [
                'body' => json_encode($parameter)
            ]);

            $body = json_decode($response->getBody()->getContents(), true);

            if (!isset($body['success']) || !$body['success']) {
                return $this->err(
                    __('dujiaoka.prompt.abnormal_payment_channel') .
                    (isset($body['message']) ? $body['message'] : '')
                );
            }

            return redirect()->away($body['data']);
        } catch (RuleValidationException $exception) {
            return $this->err($exception->getMessage());
        } catch (GuzzleException $exception) {
            return $this->err($exception->getMessage());
        }
    }

    /**
     * 构造签名原文
     *
     * 规则：
     * 1. 排除 Signature
     * 2. 忽略 null 和空字符串
     * 3. 保留 0、"0"、false
     * 4. 字段名区分大小写升序
     * 5. bool -> true / false
     * 6. key=value&key=value
     * 7. 不进行 URL 编码
     */
    private function buildCanonicalParameters(array $parameter)
    {
        unset($parameter['Signature']);
        ksort($parameter, SORT_STRING);

        $pairs = [];

        foreach ($parameter as $key => $value) {
            if ($value === null || $value === '') {
                continue;
            }

            if (is_bool($value)) {
                $value = $value ? 'true' : 'false';
            }

            $pairs[] = $key . '=' . $value;
        }

        return implode('&', $pairs);
    }

    /**
     * 生成签名
     *
     * MD5:
     * md5(canonicalParameters . signKey)
     *
     * HMAC-SHA256:
     * hash_hmac('sha256', canonicalParameters, signKey)
     */
    private function generateSign(array $parameter, string $signKey)
    {
        $canonicalParameters = $this->buildCanonicalParameters($parameter);

        if (self::SIGNATURE_ALGORITHM === 'HmacSha256') {
            return hash_hmac('sha256', $canonicalParameters, $signKey);
        }

        return md5($canonicalParameters . $signKey);
    }

    public function notifyUrl(Request $request)
    {
        /*
         * 直接使用 Laravel 解析后的全部请求参数。
         * 验签不假设固定业务字段：
         * 实际传过来什么字段，就验证什么字段。
         * 唯一固定排除的是 Signature。
         */
        $data = $request->all();

        if (!is_array($data) || !isset($data['Signature']) || !is_string($data['Signature'])) {
            return 'fail';
        }

        if (!isset($data['OutOrderId'])) {
            return 'fail';
        }

        $order = $this->orderService->detailOrderSN($data['OutOrderId']);

        if (!$order) {
            return 'fail';
        }

        $payGateway = $this->payService->detail($order->pay_id);

        if (!$payGateway) {
            return 'fail';
        }

        $receivedSignature = strtolower($data['Signature']);
        $expectedSignature = $this->generateSign($data, $payGateway->merchant_key);

        if (!hash_equals($expectedSignature, $receivedSignature)) {
            return 'fail';
        }

        // 0 = Pending
        // 1 = Paid
        // 2 = Expired
        if (!array_key_exists('Status', $data) || $data['Status'] != 1) {
            return 'fail';
        }

        if (!isset($data['ActualAmount'], $data['Id'])) {
            return 'fail';
        }

        $this->orderProcessService->completedOrder(
            $data['OutOrderId'],
            $data['ActualAmount'],
            $data['Id']
        );

        return 'ok';
    }

    public function returnUrl(Request $request)
    {
        $oid = $request->get('order_id');

        sleep(2);

        return redirect(url('detail-order-sn', ['orderSN' => $oid]));
    }
}