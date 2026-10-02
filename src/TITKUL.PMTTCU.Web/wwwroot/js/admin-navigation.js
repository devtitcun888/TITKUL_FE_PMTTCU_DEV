(() => {
    const shell = document.querySelector('.admin-shell');
    const nav = document.querySelector('.side-nav');
    if (!nav) return;

    const mobileToggle = nav.querySelector('.side-nav-mobile-toggle');
    const backdrop = document.querySelector('[data-admin-backdrop]');
    const collapseButton = document.querySelector('[data-admin-collapse]');
    const collapseKey = 'pmttcu.adminNavCollapsed';
    const mobileQuery = window.matchMedia('(max-width: 1100px)');

    const setBackdrop = (open) => {
        if (!backdrop) return;
        backdrop.hidden = !open;
    };

    const closeMobileMenu = () => {
        nav.classList.remove('is-mobile-open');
        shell?.classList.remove('is-mobile-open');
        mobileToggle?.setAttribute('aria-expanded', 'false');
        if (mobileToggle) mobileToggle.textContent = 'Mở menu';
        setBackdrop(false);
    };

    const openMobileMenu = () => {
        nav.classList.add('is-mobile-open');
        shell?.classList.add('is-mobile-open');
        mobileToggle?.setAttribute('aria-expanded', 'true');
        if (mobileToggle) mobileToggle.textContent = 'Đóng menu';
        setBackdrop(true);
    };

    const applyCollapse = () => {
        if (!shell) return;
        if (mobileQuery.matches) {
            shell.classList.remove('admin-shell-collapsed');
            return;
        }
        if (localStorage.getItem(collapseKey) === '1') {
            shell.classList.add('admin-shell-collapsed');
        } else {
            shell.classList.remove('admin-shell-collapsed');
        }
    };

    mobileToggle?.addEventListener('click', () => {
        if (nav.classList.contains('is-mobile-open')) closeMobileMenu();
        else openMobileMenu();
    });
    backdrop?.addEventListener('click', closeMobileMenu);
    nav.addEventListener('click', event => {
        if (event.target.closest('.side-nav-body a') && mobileQuery.matches)
            closeMobileMenu();
    });
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape') closeMobileMenu();
    });
    collapseButton?.addEventListener('click', () => {
        if (mobileQuery.matches) {
            if (nav.classList.contains('is-mobile-open')) closeMobileMenu();
            else openMobileMenu();
            return;
        }
        const collapsed = shell.classList.toggle('admin-shell-collapsed');
        localStorage.setItem(collapseKey, collapsed ? '1' : '0');
    });
    mobileQuery.addEventListener('change', () => {
        closeMobileMenu();
        applyCollapse();
    });
    applyCollapse();

    const groups = [...nav.querySelectorAll('.side-nav-group')];
    groups.forEach(group => group.setAttribute('name', 'admin-navigation-groups'));

    const currentPath = window.location.pathname.replace(/\/$/, '').toLowerCase() || '/';
    const links = [...nav.querySelectorAll('a[href^="/admin/"]')];
    const matchesPath = (href) => {
        const target = new URL(href, window.location.origin).pathname.replace(/\/$/, '').toLowerCase();
        return currentPath === target || currentPath.startsWith(`${target}/`);
    };
    const activeLink = links
        .filter(link => matchesPath(link.getAttribute('href')))
        .sort((a, b) => b.getAttribute('href').length - a.getAttribute('href').length)[0];

    if (activeLink) {
        activeLink.classList.add('is-current');
        activeLink.closest('.side-nav-group')?.classList.add('has-current');
        activeLink.closest('.side-nav-group')?.setAttribute('open', '');
    }

    const search = nav.querySelector('#admin-nav-search');
    const status = nav.querySelector('.side-nav-search-status');
    if (!search) return;

    const filterMenu = () => {
        const term = search.value.trim().toLocaleLowerCase('vi');
        let visibleCount = 0;

        links.forEach(link => {
            const isUtility = link.classList.contains('side-nav-logout') || link.classList.contains('admin-account-button');
            const visible = isUtility || !term || link.textContent.trim().toLocaleLowerCase('vi').includes(term);
            link.hidden = !visible;
            if (visible && !isUtility) visibleCount++;
        });

        groups.forEach(group => {
            const visibleItems = [...group.querySelectorAll('.side-nav-items a')].some(link => !link.hidden);
            group.hidden = term.length > 0 && !visibleItems;
            if (term) {
                group.removeAttribute('name');
                group.toggleAttribute('open', visibleItems);
            } else {
                group.setAttribute('name', 'admin-navigation-groups');
                group.toggleAttribute('open', group.classList.contains('has-current'));
            }
        });

        nav.querySelectorAll(':scope > a[href^="/admin/"], .side-nav-body > a[href^="/admin/"]').forEach(link => {
            if (link.classList.contains('side-nav-logout') || link.classList.contains('admin-account-button')) return;
            const visible = !term || link.textContent.trim().toLocaleLowerCase('vi').includes(term);
            link.hidden = !visible;
        });

        if (status) status.textContent = term
            ? (visibleCount ? `${visibleCount} mục phù hợp` : 'Không tìm thấy chức năng.')
            : '';
    };

    search.addEventListener('input', filterMenu);
    filterMenu();
})();
