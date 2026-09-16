<?php

namespace Gateway\Pay\TokenPay;

use Gateway\Pay\ApiInterface;
use GuzzleHttp\Client;
use GuzzleHttp\Exception\GuzzleException;

class Api implements ApiInterface
{
    private $url_notify = '';
    private $url_return = '';

    public function __construct($id)
    {
        $this->url_notify = SYS_URL_API . '/pay/notify/' . $id;
        $this->url_return = SYS_URL . '/pay/return/' . $id;
    }

    function goPay($config, $out_trade_no, $subject, $body, $amount_cent)
    {
        try {
            $payway = $config['payway'];
            $gateway = $config['gateway'];
            $api_key = $config['api_key'];
            $algorithm = $this->getSignatureAlgorithm($config);

            $order = \App\Order::whereOrderNo($out_trade_no)->first();

            $parameter = [
                'ActualAmount' => $amount_cent / 100,
                'OutOrderId' => $out_trade_no,
                'OrderUserKey' => $order->contact,
                'Currency' => $payway,
                'RedirectUrl' => $this->url_return . '/' . $out_trade_no,
                'NotifyUrl' => $this->url_notify,
            ];

            $parameter['Signature'] = $this->tokenPaySign($parameter, $api_key, $algorithm);

            $client = new Client([
                'headers' => ['Content-Type' => 'application/json']
            ]);

            $response = $client->post($gateway . '/CreateOrder', [
                'body' => json_encode($parameter)
            ]);

            $result = json_decode($response->getBody()->getContents(), true);

            if (!isset($result['success']) || !$result['success']) {
                throw new \Exception('支付网关异常' . (isset($result['message']) ? $result['message'] : ''));
            }

            header('Location: ' . $result['data']);
            exit;
        } catch (GuzzleException $exception) {
            throw new \Exception($exception->getMessage());
        }
    }

    private function getSignatureAlgorithm(array $config)
    {
        $value = isset($config['signature_algorithm']) ? (string)$config['signature_algorithm'] : '0';

        return $value === '1' ? 'HmacSha256' : 'MD5';
    }

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

    private function tokenPaySign(array $parameter, string $signKey, $algorithm = 'MD5')
    {
        $canonicalParameters = $this->buildCanonicalParameters($parameter);

        if (
            strcasecmp($algorithm, 'HmacSha256') === 0 ||
            strcasecmp($algorithm, 'HMAC-SHA256') === 0 ||
            strcasecmp($algorithm, 'HMACSHA256') === 0 ||
            (string)$algorithm === '1'
        ) {
            return hash_hmac('sha256', $canonicalParameters, $signKey);
        }

        return md5($canonicalParameters . $signKey);
    }

    function verify($config, $successCallback)
    {
        $isNotify = isset($config['isNotify']) && $config['isNotify'];

        if ($isNotify) {
            $api_key = $config['api_key'];
            $algorithm = $this->getSignatureAlgorithm($config);

            $raw = file_get_contents('php://input');
            $data = json_decode($raw, true);

            if (!is_array($data) || !isset($data['Signature']) || !is_string($data['Signature'])) {
                echo 'error';
                return false;
            }

            $receivedSignature = strtolower($data['Signature']);
            $expectedSignature = $this->tokenPaySign($data, $api_key, $algorithm);

            if (!hash_equals($expectedSignature, $receivedSignature)) {
                echo 'error';
                return false;
            }

            if (!array_key_exists('Status', $data) || $data['Status'] != 1) {
                echo 'error';
                return false;
            }

            if (!isset($data['OutOrderId'], $data['ActualAmount'], $data['Id'])) {
                echo 'error';
                return false;
            }

            $order_no = $data['OutOrderId'];
            $total_fee = $data['ActualAmount'] * 100;
            $pay_trade_no = $data['Id'];

            $successCallback($order_no, $total_fee, $pay_trade_no);

            echo 'ok';
            return true;
        }

        if (!empty($_GET['OutOrderId'])) {
            sleep(2);

            $order_id = $_GET['OutOrderId'];
            $order = \App\Order::whereOrderNo($order_id)->first();

            if ($order && $order->status > 0) {
                return true;
            }
        }

        return false;
    }

    function refund($config, $order_no, $pay_trade_no, $amount_cent)
    {
        return '此支付渠道不支持发起退款, 请手动操作';
    }
}