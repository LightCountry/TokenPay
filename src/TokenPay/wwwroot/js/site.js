(function () {
    'use strict'
    // Tooltip for Bootstrap.
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(tooltip => {
        new bootstrap.Tooltip(tooltip);
    });

    // Copy to clipboard.
    const clipboard = new ClipboardJS('.btn-clipboard', {
        text: trigger => trigger.getAttribute('data-clipboard-text').trim()
    });
    clipboard.on('success', event => {
        const tooltipBtn = bootstrap.Tooltip.getInstance(event.trigger);
        const originalTitle = event.trigger.title;
        tooltipBtn.setContent({ '.tooltip-inner': 'Copied!' });
        event.trigger.addEventListener('hidden.bs.tooltip', () => {
            tooltipBtn.setContent({ '.tooltip-inner': 'Copy to clipboard' })
        }, { once: true });
        event.clearSelection();
        setTimeout(() => {
            event.trigger.title = originalTitle
        }, 2000);
    });

    // Countdown for payment.
    const countdownCircleProgress = document.getElementById('countdown-circle-progress');
    if (!!countdownCircleProgress) {
        const totalSeconds = +countdownCircleProgress.getAttribute('data-total-seconds');
        const remainSeconds = +countdownCircleProgress.getAttribute('data-remain-seconds');

        let circleProgress = new CircleProgress('#countdown-circle-progress', {
            max: totalSeconds,
            value: remainSeconds,
            animation: 'none',
            textFormat: function (value, max) {
                const minutes = Math.floor(value / 60).toFixed().padStart(2, '0');
                const seconds = Math.floor(value - minutes * 60).toFixed().padStart(2, '0');
                return minutes + ':' + seconds;
            },
        });
        let lineProgress = document.getElementById('countdown-progress-bar');
        setInterval(() => {
            circleProgress.attr('value', circleProgress.value - 1);
            lineProgress.style.width = (circleProgress.value / totalSeconds * 100).toString() + '%';
        }, 1000);

        const orderId = countdownCircleProgress.getAttribute('data-order-id');
        // I do not think it is reasonable to request the API every ONE second.
        // Here I have reduced the frequency of polling.
        setInterval(() => {
            fetch(`/Check/${orderId}`)
                .then(response => response.text())
                .then(status => {
                    switch (status) {
                        case 'Pending':
                            // Igonre.
                            break;
                        case 'Expired':
                            window.location.reload();
                            break;
                        case 'Paid':
                            const redirectUrl = countdownCircleProgress.getAttribute('data-redirect-url');
                            window.location = redirectUrl;
                            break;
                        default:
                            // Igonre.
                            break;
                    }
                })
                .catch(error => {
                    // TODO: Notify the error.
                    console.error(error);
                });
        }, 5000);
    }

    // Copyright.
    console.log("Powered by TokenPay");
    console.log("https://github.com/LightCountry/TokenPay");
})()