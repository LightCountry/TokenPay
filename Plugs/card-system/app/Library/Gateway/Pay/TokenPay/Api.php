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


    /**
     * @param array $config 配置信息
     * @param string $out_trade_no 发卡系统订单号
     * @param string $subject 商品名称
     * @param string $body 商品介绍
     * @param int $amount_cent 支付金额, 单位:分
     * @throws \Exception
     */
    function goPay($config, $out_trade_no, $subject, $body, $amount_cent)
    {
        try {
            // 加载网关
            $payway = $config['payway'];
            $gateway = $config['gateway'];
            $api_key = $config['api_key'];
            $order = \App\Order::whereOrderNo($out_trade_no)->first();
            //构造要请求的参数数组，无需改动
            $parameter = [
                "ActualAmount" => $amount_cent/100,
                "OutOrderId" => $out_trade_no, 
                "OrderUserKey" => $order->contact, 
                "Currency" => $payway,
                'RedirectUrl' => $this->url_return.'/'.$out_trade_no,
                'NotifyUrl' => $this->url_notify,
            ];
            $parameter['Signature'] = $this->tokenPaySign($parameter, $api_key );
            $client = new Client([
                'headers' => [ 'Content-Type' => 'application/json' ]
            ]);
            $response = $client->post($gateway.'/CreateOrder', ['body' => json_encode($parameter)]);
            $body = json_decode($response->getBody()->getContents(), true);
            if (!isset($body['success']) || !$body['success']) {
                throw new \Exception('支付网关异常' . $body['message']);
            }
            header('Location: '.$body['data']);
            exit;
        } catch (GuzzleException $exception) {
            throw new \Exception($exception->getMessage());
        }
    }
    private function tokenPaySign(array $parameter, string $signKey)
    {
        ksort($parameter);
        reset($parameter); //内部指针指向数组中的第一个元素
        $sign = '';
        $urls = '';
        foreach ($parameter as $key => $val) {
            if ($val == '') continue;
            if ($key != 'Signature') {
                if ($sign != '') {
                    $sign .= "&";
                    $urls .= "&";
                }
                $sign .= "$key=$val"; //拼接为url参数形式
                $urls .= "$key=" . urlencode($val); //拼接为url参数形式
            }
        }
        $sign = md5($sign . $signKey);//密码追加进入开始MD5签名
        return $sign;
    }
    /**
     * @param $config
     * @param callable $successCallback
     * @return bool|string
     * @throws \Exception
     */
    function verify($config, $successCallback)
    {
        $isNotify = isset($config['isNotify']) && $config['isNotify'];
        // 如果是异步通知
        if ($isNotify) {
            $api_key = $config['api_key'];
            $data = \Request()->all();
            if ($this->tokenPaySign($_POST,$api_key)) {
                $order_no = $data['OutOrderId'];  // 发卡系统内交易单号
                $total_fee = $data['ActualAmount']*100; // 实际支付金额, 单位, 分
                $pay_trade_no = $data['Id']; // 支付系统内订单号/流水号
                $successCallback($order_no, $total_fee, $pay_trade_no); 
                echo 'ok';
                return true;
            } else {
                echo 'error';
                return false;
            }
        }
        if (!empty($_GET['OutOrderId'])) {
            sleep(2);
            $order_id = $_GET['OutOrderId'];
            $order = \App\Order::whereOrderNo($order_id)->first();
            if($order->status>0) return true;
        }
        return false;
    }
    /**
     * 退款操作
     * @param array $config 支付渠道配置
     * @param string $order_no 订单号
     * @param string $pay_trade_no 支付渠道流水号
     * @param int $amount_cent 金额/分
     * @return true|string true 退款成功  string 失败原因
     */
    function refund($config, $order_no, $pay_trade_no, $amount_cent)
    {
        return '此支付渠道不支持发起退款, 请手动操作';
    }
}