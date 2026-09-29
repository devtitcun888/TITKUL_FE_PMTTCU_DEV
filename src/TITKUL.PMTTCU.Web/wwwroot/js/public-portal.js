(() => {
    const navToggle = document.querySelector('.nav-toggle');
    const mainNavigation = document.querySelector('#main-navigation');
    navToggle?.addEventListener('click', function () {
        const expanded = this.getAttribute('aria-expanded') === 'true';
        this.setAttribute('aria-expanded', String(!expanded));
        mainNavigation?.classList.toggle('is-open', !expanded);
    });

    document.addEventListener('keydown', (event) => {
        if (event.key !== 'Escape' || !(event.target instanceof Element)) return;
        const openMenu = event.target.closest('details[open]') ?? mainNavigation?.querySelector('details[open]');
        if (openMenu) {
            openMenu.removeAttribute('open');
            openMenu.querySelector('summary')?.focus();
            event.preventDefault();
            return;
        }

        if (mainNavigation.classList.contains('is-open')) {
            mainNavigation.classList.remove('is-open');
            navToggle?.setAttribute('aria-expanded', 'false');
            navToggle?.focus();
            event.preventDefault();
        }
    });

    const portalClock = document.querySelector('#portal-clock');
    const updatePortalClock = () => {
        if (!portalClock) return;
        portalClock.textContent = new Intl.DateTimeFormat('vi-VN', {
            weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric',
            hour: '2-digit', minute: '2-digit', second: '2-digit', timeZone: 'Asia/Ho_Chi_Minh'
        }).format(new Date());
    };

    updatePortalClock();
    window.setInterval(updatePortalClock, 1000);

    const backToTop = document.querySelector('.back-to-top');
    if (backToTop) {
        const updateBackToTop = () => { backToTop.hidden = window.scrollY < 480; };
        window.addEventListener('scroll', updateBackToTop, { passive: true });
        updateBackToTop();
    }
})();
