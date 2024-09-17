<?php

class TokenPay_plugin{
	static public $info = [
		'name'        => 'TokenPay', //支付插件英文名称，需和目录名称一致，不能有重复
		'showname'    => 'TokenPay', //支付插件显示名称
		'author'      => 'TokenPay', //支付插件作者
		'link'        => 'https://github.com/LightCountry/TokenPay', //支付插件作者链接
		'types'       => ['TRX', 'USDT_TRC20', 'EVM_ETH_ETH', 'EVM_ETH_USDT_ERC20', 'EVM_ETH_USDC_ERC20', 'EVM_BSC_BNB', 'EVM_BSC_USDT_BEP20', 'EVM_BSC_USDC_BEP20', 'EVM_Polygon_POL', 'EVM_Polygon_USDT_ERC20', 'EVM_Polygon_USDC_ERC20'], //支付插件支持的支付方式，可选的有alipay,qqpay,wxpay,bank
		'inputs' => [ //支付插件要求传入的参数以及参数显示名称，可选的有appid,appkey,appsecret,appurl,appmchid
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
		],
		'select' => null,
		'note' => '', //支付密钥填写说明
		'bindwxmp' => false, //是否支持绑定微信公众号
		'bindwxa' => false, //是否支持绑定微信小程序
	];

	static public function submit(){
		global $siteurl, $channel, $order, $sitename;
		
		if(in_array($order['typename'], self::$info['types'])){
			return ['type'=>'jump','url'=>'/pay/TokenPay/'.TRADE_NO.'/?sitename='.$sitename];
		}
	}

	static public function mapi(){
		global $siteurl, $channel, $order, $device, $mdevice;

		if(in_array($order['typename'], self::$info['types'])){
			return self::TokenPay($order['typename']);
		}
	}

	static private function getApiUrl(){
		global $channel;
		$apiurl = $channel['appurl'];
		if(substr($apiurl, -1, 1) == '/')$apiurl = substr($apiurl, 0, -1);
		return $apiurl;
	}

	static private function sendRequest($url, $param, $key){
		$url = self::getApiUrl().$url;
		$post = json_encode($param);
		$response = get_curl($url,$post,0,0,0,0,0,['Content-Type: application/json']);

		return json_decode($response, true);
	}

    static public function Sign($params,$appKey){
        if(!empty($params)){
           $p =  ksort($params);
           if($p){
               $str = '';
               foreach ($params as $k=>$val){
                   $str .= $k .'=' . $val . '&';
               }
               $strs = rtrim($str, '&').$appKey;

               return md5($strs);
           }
        }
        
        return null;
    }
    
    
    
	//通用创建订单
	static private function CreateOrder($type, $extra = null){
		global $siteurl, $channel, $order, $ordername, $conf, $clientip;
		echo $type;
		$param = [
		    'OutOrderId' => TRADE_NO, //外部订单号
		    'OrderUserKey' => (string)$order['uid'],   //支付用户标识
		    'ActualAmount' => $order['realmoney'],   //订单实际支付的人民币金额，保留两位小数
		    'Currency' => $order['typename'],   //加密货币的币种，直接以原样字符串传递即可
		    'NotifyUrl' => $conf['localurl'].'pay/notify/'.TRADE_NO.'/',  //异步通知URL
		    'RedirectUrl' => $siteurl.'pay/return/'.TRADE_NO.'/'    //订单支付或过期后跳转的URL
		];
		
		if($extra){
			$param = array_merge($param, $extra);
		}
        $param['Signature'] = self::Sign($param,$channel['appkey']); //参数签名

		$result = self::sendRequest('/CreateOrder', $param, $channel['appkey']);
		

		if(isset($result["success"]) && $result["success"]){
			\lib\Payment::updateOrder(TRADE_NO, $result['data']);
			$code_url = $result['data'];
		}else{
			throw new Exception($result["message"]?$result["message"]:'返回数据解析失败');
		}
		return $code_url;
	}

    
    static public function TokenPay(){
		try{
			$code_url = self::CreateOrder('');
		}catch(Exception $ex){
			return ['type'=>'error','msg'=>'TokenPay创建订单失败！'.$ex->getMessage()];
		}

        return ['type'=>'jump','url'=>$code_url];
	}

	//异步回调
	static public function notify(){
		global $channel, $order;

		$resultJson = file_get_contents("php://input");
		$resultArr = json_decode($resultJson,true);
		$Signature = $resultArr["Signature"];
		
		//生成签名时取出 Signature 字段
		unset($resultArr['Signature']);
		
		$sign = self::Sign($resultArr,$channel['appkey']);
    
		if($sign===$Signature){
			$out_trade_no = $resultArr['OutOrderId'];

			if ($out_trade_no == TRADE_NO) {
				processNotify($order, $out_trade_no);
			}else{
			    return ['type'=>'html','data'=>'fail'];
			}
			return ['type'=>'html','data'=>'ok'];
		}else{
			return ['type'=>'html','data'=>'fail'];
		}
	}

	//支付返回页面
	static public function return(){
		return ['type'=>'page','page'=>'return'];
	}
}
