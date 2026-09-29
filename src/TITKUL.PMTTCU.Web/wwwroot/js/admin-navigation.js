(() => {
    const nav = document.querySelector('.side-nav');
    if (!nav) return;

    const mobileToggle = nav.querySelector('.side-nav-mobile-toggle');
    const closeMobileMenu = () => {
        nav.classList.remove('is-mobile-open');
        mobileToggle?.setAttribute('aria-expanded', 'false');
        if (mobileToggle) mobileToggle.textContent = 'Mở menu';
    };

    mobileToggle?.addEventListener('click', () => {
        const isOpen = nav.classList.toggle('is-mobile-open');
        mobileToggle.setAttribute('aria-expanded', String(isOpen));
        mobileToggle.textContent = isOpen ? 'Đóng menu' : 'Mở menu';
    });
    nav.addEventListener('click', event => {
        if (event.target.closest('.side-nav-body a') && window.matchMedia('(max-width: 768px)').matches)
            closeMobileMenu();
    });
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape') closeMobileMenu();
    });

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
            const visible = !term || link.textContent.trim().toLocaleLowerCase('vi').includes(term);
            link.hidden = !visible;
            if (visible) visibleCount++;
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

        nav.querySelectorAll(':scope > a[href^="/admin/"]').forEach(link => {
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
