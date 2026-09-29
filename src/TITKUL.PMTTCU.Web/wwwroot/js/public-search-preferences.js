(() => {
    const root = document.querySelector('[data-search-preferences]');
    if (!root) return;

    const storageKey = 'pmt.public-search.preferences.v1';
    const checkboxes = Array.from(root.querySelectorAll('[data-search-preference]'));
    const results = Array.from(document.querySelectorAll('[data-search-category]'));
    const reset = root.querySelector('[data-search-preferences-reset]');
    const originalOrder = new Map(results.map((element, index) => [element, index]));
    const allowed = new Set(checkboxes.map((checkbox) => checkbox.value));

    const readPreferences = () => {
        try {
            const value = JSON.parse(window.localStorage.getItem(storageKey) || '[]');
            return Array.isArray(value) ? value.filter((key) => typeof key === 'string' && allowed.has(key)) : [];
        } catch {
            return [];
        }
    };

    const savePreferences = () => {
        const selected = checkboxes.filter((checkbox) => checkbox.checked).map((checkbox) => checkbox.value);
        try {
            window.localStorage.setItem(storageKey, JSON.stringify(selected));
        } catch {
            // Search remains usable when browser storage is unavailable.
        }
        orderResults(selected);
    };

    const orderResults = (selected) => {
        const rank = new Map(selected.map((key, index) => [key, index]));
        results
            .slice()
            .sort((left, right) => {
                const leftKey = left.dataset.searchCategory || '';
                const rightKey = right.dataset.searchCategory || '';
                const leftRank = rank.has(leftKey) ? rank.get(leftKey) : Number.MAX_SAFE_INTEGER;
                const rightRank = rank.has(rightKey) ? rank.get(rightKey) : Number.MAX_SAFE_INTEGER;
                return leftRank - rightRank || originalOrder.get(left) - originalOrder.get(right);
            })
            .forEach((element) => element.parentElement.append(element));
    };

    const preferences = readPreferences();
    checkboxes.forEach((checkbox) => {
        checkbox.checked = preferences.includes(checkbox.value);
        checkbox.addEventListener('change', savePreferences);
    });
    orderResults(preferences);

    reset?.addEventListener('click', () => {
        checkboxes.forEach((checkbox) => { checkbox.checked = false; });
        try {
            window.localStorage.removeItem(storageKey);
        } catch {
            // Reset the current page even when browser storage is unavailable.
        }
        orderResults([]);
    });
})();
