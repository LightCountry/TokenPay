<?php

class TokenPay_plugin
{
    static public $info = [
        'name' => 'TokenPay',
        'showname' => 'TokenPay',
        'author' => 'TokenPay',
        'link' => 'https://github.com/LightCountry/TokenPay',
        'types' => [
            'TRX',
            'USDT_TRC20',
            'EVM_ETH_ETH',
            'EVM_ETH_USDT_ERC20',
            'EVM_ETH_USDC_ERC20',
            'EVM_BSC_BNB',
            'EVM_BSC_USDT_BEP20',
            'EVM_BSC_USDC_BEP20',
            'EVM_Polygon_POL',
            'EVM_Polygon_USDT_ERC20',
            'EVM_Polygon_USDC_ERC20'
        ],
        'inputs' => [
            'appurl' => [
                'name' => 'API接口地址',
                'type' => 'input',
                'note' => '以http://或https://开头，末尾不要有斜线/',
            ],
            'appid' => [
                'name' => 'APP ID',
                'type' => 'input',
                'note' => '输入任意字符即可',
            ],
            'appkey' => [
                'name' => 'API秘钥',
                'type' => 'input',
                'note' => 'TokenPay API 密钥',
            ],
            'signature_algorithm' => [
                'name' => '签名算法',
                'type' => 'select',
                'options' => [
                    '0' => 'MD5（兼容模式）',
                    '1' => 'HMAC-SHA256（推荐）',
                ],
                'note' => '请选择与 TokenPay 服务端配置一致的签名算法',
            ],
        ],
        'select' => null,
        'note' => '',
        'bindwxmp' => false,
        'bindwxa' => false,
    ];

    static public function submit()
    {
        global $order, $sitename;

        if (in_array($order['typename'], self::$info['types'], true)) {
            return [
                'type' => 'jump',
                'url' => '/pay/TokenPay/' . TRADE_NO . '/?sitename=' . $sitename
            ];
        }
    }

    static public function mapi()
    {
        global $order;

        if (in_array($order['typename'], self::$info['types'], true)) {
            return self::TokenPay();
        }
    }

    static private function getApiUrl()
    {
        global $channel;

        return rtrim((string)$channel['appurl'], '/');
    }

    static private function sendRequest($url, $param)
    {
        $response = get_curl(
            self::getApiUrl() . $url,
            json_encode($param),
            0,
            0,
            0,
            0,
            0,
            ['Content-Type: application/json']
        );

        $result = json_decode($response, true);

        if (!is_array($result)) {
            throw new Exception('TokenPay 返回数据格式错误');
        }

        return $result;
    }

    static private function sign(array $data): string
    {
        global $channel;

        unset($data['Signature']);
        $data = array_filter($data, static fn ($value): bool => $value !== null && $value !== '');
        ksort($data, SORT_STRING);

        $canonical = implode('&', array_map(
            static fn ($key, $value): string =>
                $key . '=' . (is_bool($value) ? ($value ? 'true' : 'false') : (string)$value),
            array_keys($data),
            $data
        ));

        $token = (string)$channel['appkey'];
        $algorithm = (string)($channel['signature_algorithm'] ?? '0');

        return $algorithm === '1'
            ? hash_hmac('sha256', $canonical, $token)
            : md5($canonical . $token);
    }

    static private function verify(array $data): bool
    {
        $signature = (string)($data['Signature'] ?? '');

        return $signature !== '' && hash_equals(self::sign($data), strtolower($signature));
    }

    static private function CreateOrder($type, $extra = null)
    {
        global $siteurl, $channel, $order, $conf;

        echo $type;

        $param = [
            'OutOrderId' => TRADE_NO,
            'OrderUserKey' => (string)$order['uid'],
            'ActualAmount' => $order['realmoney'],
            'Currency' => $order['typename'],
            'NotifyUrl' => $conf['localurl'] . 'pay/notify/' . TRADE_NO . '/',
            'RedirectUrl' => $siteurl . 'pay/return/' . TRADE_NO . '/'
        ];

        if (is_array($extra) && !empty($extra)) {
            $param = array_merge($param, $extra);
        }

        $param['Signature'] = self::sign($param);
        $result = self::sendRequest('/CreateOrder', $param);

        if (empty($result['success'])) {
            throw new Exception((string)($result['message'] ?? 'TokenPay 创建订单失败'));
        }

        $url = (string)($result['data'] ?? '');

        if ($url === '') {
            throw new Exception('TokenPay 未返回支付链接');
        }

        \lib\Payment::updateOrder(TRADE_NO, $url);

        return $url;
    }

    static public function TokenPay()
    {
        try {
            return [
                'type' => 'jump',
                'url' => self::CreateOrder('')
            ];
        } catch (Exception $e) {
            return [
                'type' => 'error',
                'msg' => 'TokenPay创建订单失败！' . $e->getMessage()
            ];
        }
    }

    static public function notify()
    {
        global $order;

        $data = json_decode(file_get_contents('php://input'), true);

        if (!is_array($data) || !self::verify($data)) {
            return self::acknowledge(false);
        }

        if ((string)($data['OutOrderId'] ?? '') !== (string)TRADE_NO) {
            return self::acknowledge(false);
        }

        if ((int)($data['Status'] ?? 0) !== 1) {
            return self::acknowledge(false);
        }

        processNotify($order, (string)$data['OutOrderId']);

        return self::acknowledge(true);
    }

    static private function acknowledge(bool $success): array
    {
        return [
            'type' => 'html',
            'data' => $success ? 'ok' : 'fail'
        ];
    }

    static public function return()
    {
        return [
            'type' => 'page',
            'page' => 'return'
        ];
    }
}