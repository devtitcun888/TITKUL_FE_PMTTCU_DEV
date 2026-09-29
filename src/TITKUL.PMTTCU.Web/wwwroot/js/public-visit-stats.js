(() => {
    if (navigator.doNotTrack === '1' || navigator.globalPrivacyControl === true) return;

    const send = (pageView) => {
        const payload = JSON.stringify({ pageView });
        if (navigator.sendBeacon) {
            navigator.sendBeacon('/public-visit', new Blob([payload], { type: 'application/json' }));
            return;
        }
        fetch('/public-visit', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: payload,
            keepalive: true,
            credentials: 'same-origin'
        }).catch(() => {});
    };

    send(true);
    window.setInterval(() => {
        if (document.visibilityState === 'visible') send(false);
    }, 60_000);
})();
