(() => {
    const slugify = value => {
        if (!String(value ?? '').trim()) return '';
        const normalized = String(value ?? '')
            .replace(/[đĐ]/g, character => character === 'đ' ? 'd' : 'D')
            .normalize('NFD')
            .replace(/\p{Diacritic}/gu, '')
            .toLocaleLowerCase('vi');
        const slug = normalized.replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '').slice(0, 80).replace(/-+$/g, '');
        return slug.length >= 2 ? slug : 'bai-viet';
    };

    const makeDescription = value => {
        const text = String(value ?? '').replace(/\s+/g, ' ').trim();
        if (text.length <= 170) return text;
        const shortened = text.slice(0, 170);
        const lastSpace = shortened.lastIndexOf(' ');
        return `${shortened.slice(0, lastSpace > 120 ? lastSpace : 170).trimEnd()}…`;
    };

    document.querySelectorAll('form').forEach(form => {
        const title = form.querySelector('[data-slug-source]');
        const slug = form.querySelector('[data-slug-target]');
        const preview = form.querySelector('[data-slug-preview]');
        let slugWasEdited = Boolean(slug?.value.trim());

        const renderSlug = () => {
            if (!title || !slug) return;
            if (!slugWasEdited) slug.value = slugify(title.value);
            if (preview) preview.textContent = `${slug.dataset.slugPrefix ?? ''}${slug.value}`;
        };

        if (title && slug) {
            slug.addEventListener('input', () => {
                slugWasEdited = Boolean(slug.value.trim());
                renderSlug();
            });
            title.addEventListener('input', renderSlug);
            renderSlug();
        }

        const seoTitle = form.querySelector('[data-seo-title-target]');
        const seoDescription = form.querySelector('[data-seo-description-target]');
        const summary = form.querySelector('[data-seo-description-source]');
        const seoTitleCount = form.querySelector('[data-seo-title-count]');
        const seoDescriptionCount = form.querySelector('[data-seo-description-count]');
        let suggestedTitle = '';
        let suggestedDescription = '';
        let titleWasEdited = Boolean(seoTitle?.value.trim());
        let descriptionWasEdited = Boolean(seoDescription?.value.trim());

        const updateSeo = () => {
            if (seoTitle) {
                if (!titleWasEdited) {
                    suggestedTitle = (title?.value ?? '').trim().slice(0, 70);
                    seoTitle.value = suggestedTitle;
                }
                if (seoTitleCount) seoTitleCount.textContent = `${seoTitle.value.length}/70 ký tự`;
            }
            if (seoDescription) {
                if (!descriptionWasEdited) {
                    suggestedDescription = makeDescription(summary?.value || '');
                    seoDescription.value = suggestedDescription;
                }
                if (seoDescriptionCount) seoDescriptionCount.textContent = `${seoDescription.value.length}/170 ký tự`;
            }
        };

        if (seoTitle) {
            seoTitle.addEventListener('input', () => {
                titleWasEdited = Boolean(seoTitle.value.trim() && seoTitle.value !== suggestedTitle);
                updateSeo();
            });
        }
        if (seoDescription) {
            seoDescription.addEventListener('input', () => {
                descriptionWasEdited = Boolean(seoDescription.value.trim() && seoDescription.value !== suggestedDescription);
                updateSeo();
            });
        }
        title?.addEventListener('input', updateSeo);
        summary?.addEventListener('input', updateSeo);
        if (seoTitle || seoDescription) updateSeo();
    });
})();
