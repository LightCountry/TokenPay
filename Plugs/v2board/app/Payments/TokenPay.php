<?php

namespace App\Payments;

use \Curl\Curl;

class TokenPay
{
    public function __construct($config)
    {
        $this->config = $config;
    }

    public function form()
    {
        return [
            'token_pay_url' => [
                'label' => 'API 地址',
                'description' => '您的 TokenPay API 接口地址，例如：https://token-pay.xxx.com',
                'type' => 'input',
            ],

            'token_pay_apitoken' => [
                'label' => 'API Token',
                'description' => '您的 TokenPay API Token',
                'type' => 'input',
            ],

            'token_pay_currency' => [
                'label' => '币种',
                'description' => '例如：USDT_TRC20、TRX',
                'type' => 'input',
            ],

            'token_pay_signature_algorithm' => [
				'label' => '签名算法',
				'description' => '0 = MD5（兼容模式），1 = HMAC-SHA256（推荐）',
				'type' => 'input',
			],
        ];
    }

    /**
     * 构建规范化签名字符串
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
    private function buildCanonicalParameters(array $params): string
    {
        unset($params['Signature']);

        ksort($params, SORT_STRING);

        $pairs = [];

        foreach ($params as $key => $value) {
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
     * 获取签名算法
     *
     * 0 = MD5
     * 1 = HMAC-SHA256
     */
    private function getSignatureAlgorithm(): string
    {
        $value = (string) (
            $this->config['token_pay_signature_algorithm'] ?? '0'
        );

        return $value === '1'
            ? 'HmacSha256'
            : 'MD5';
    }

    /**
     * 生成签名
     */
    private function sign(array $params): string
    {
        $canonicalParameters =
            $this->buildCanonicalParameters($params);

        $apiToken =
            $this->config['token_pay_apitoken'];

        switch ($this->getSignatureAlgorithm()) {
            case 'HmacSha256':
                return hash_hmac(
                    'sha256',
                    $canonicalParameters,
                    $apiToken
                );

            case 'MD5':
            default:
                return md5(
                    $canonicalParameters . $apiToken
                );
        }
    }

    public function pay($order)
    {
        $params = [
            'ActualAmount' => $order['total_amount'] / 100,
            'OutOrderId' => $order['trade_no'],
            'OrderUserKey' => strval($order['user_id']),
            'Currency' => $this->config['token_pay_currency'],
            'RedirectUrl' => $order['return_url'],
            'NotifyUrl' => $order['notify_url'],
        ];

        $params['Signature'] =
            $this->sign($params);

        $curl = new Curl();

        $curl->setUserAgent('TokenPay');
        $curl->setOpt(
            CURLOPT_SSL_VERIFYPEER,
            0
        );

        $curl->setOpt(
            CURLOPT_HTTPHEADER,
            [
                'Content-Type:application/json'
            ]
        );

        $curl->post(
            $this->config['token_pay_url'] . '/CreateOrder',
            json_encode($params)
        );

        $result = $curl->response;

        $curl->close();

        if (
            !isset($result->success) ||
            !$result->success
        ) {
            $message = isset($result->message)
                ? $result->message
                : 'Unknown error';

            abort(
                500,
                "Failed to create order. Error: {$message}"
            );
        }

        return [
            'type' => 1,
            'data' => $result->data
        ];
    }

    public function notify($params)
    {
        if (!isset($params['Signature'])) {
            die('missing signature');
        }

        $signature =
            $params['Signature'];

        $expectedSignature =
            $this->sign($params);

        if (
            !is_string($signature) ||
            !hash_equals(
                $expectedSignature,
                strtolower($signature)
            )
        ) {
            die('cannot pass verification');
        }

        if (!array_key_exists('Status', $params)) {
            die('failed');
        }

        $status =
            $params['Status'];

        // 0: Pending
        // 1: Paid
        // 2: Expired
        if ($status != 1) {
            die('failed');
        }

        return [
            'trade_no' => $params['OutOrderId'],
            'callback_no' => $params['Id'],
            'custom_result' => 'ok'
        ];
    }
}