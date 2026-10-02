(() => {
    const KEY = 'pmttcu.crmTheme';
    const root = document.documentElement;

    const readTheme = () => {
        try {
            const stored = localStorage.getItem(KEY);
            if (stored === 'dark' || stored === 'night') return stored;
        } catch {
            /* keep default */
        }
        return root.getAttribute('data-crm-theme') === 'dark' ? 'dark' : 'night';
    };

    const apply = (theme) => {
        const next = theme === 'dark' ? 'dark' : 'night';
        root.setAttribute('data-crm-theme', next);
        try { localStorage.setItem(KEY, next); } catch { /* ignore quota */ }
        const dark = next === 'dark';
        document.querySelectorAll('[data-crm-theme-toggle]').forEach((button) => {
            button.setAttribute('aria-pressed', dark ? 'true' : 'false');
            button.title = dark ? 'Chuyển sang chế độ đêm' : 'Chuyển sang chế độ tối';
            const icon = button.querySelector('[data-theme-icon]');
            if (icon) icon.textContent = dark ? 'light_mode' : 'dark_mode';
            const label = button.querySelector('[data-theme-label]');
            if (label) label.textContent = dark ? 'Chế độ đêm' : 'Chế độ tối';
        });
        document.dispatchEvent(new CustomEvent('crm-theme-change', { detail: { theme: next } }));
    };

    apply(readTheme());

    document.addEventListener('click', (event) => {
        const button = event.target.closest('[data-crm-theme-toggle]');
        if (!button) return;
        apply(readTheme() === 'dark' ? 'night' : 'dark');
    });
})();
