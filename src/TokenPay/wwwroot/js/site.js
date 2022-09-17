(function () {
    'use strict'

    var clipboard = new ClipboardJS('.btn');

    clipboard.on('success', function (e) {
        alert('复制成功！\n复制内容：' + e.text)
    });

    clipboard.on('error', function (e) {
    });

    console.log("Powered by TokenPay");
    console.log("https://github.com/LightCountry/TokenPay");
})()