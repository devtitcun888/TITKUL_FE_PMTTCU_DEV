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
        const previews = form.querySelectorAll('[data-slug-preview]');
        let slugWasEdited = Boolean(slug?.value.trim());

        const renderSlug = () => {
            if (!title || !slug) return;
            if (!slugWasEdited) slug.value = slugify(title.value);
            const full = `${slug.dataset.slugPrefix ?? ''}${slug.value}`;
            previews.forEach(preview => {
                preview.textContent = preview.hasAttribute('data-slug-preview-bare') ? (slug.value || '') : full;
            });
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
        const seoTitleCounts = form.querySelectorAll('[data-seo-title-count]');
        const seoDescriptionCounts = form.querySelectorAll('[data-seo-description-count]');
        let suggestedTitle = '';
        let suggestedDescription = '';
        let titleWasEdited = Boolean(seoTitle?.value.trim());
        let descriptionWasEdited = Boolean(seoDescription?.value.trim());

        const writeCount = (nodes, length, max) => {
            nodes.forEach(el => {
                el.textContent = el.hasAttribute('data-seo-count-short') ? `${length}/${max}` : `${length}/${max} ký tự`;
            });
        };

        const updateSeo = () => {
            if (seoTitle) {
                if (!titleWasEdited) {
                    suggestedTitle = (title?.value ?? '').trim().slice(0, 70);
                    seoTitle.value = suggestedTitle;
                }
                writeCount(seoTitleCounts, seoTitle.value.length, 70);
            }
            if (seoDescription) {
                if (!descriptionWasEdited) {
                    suggestedDescription = makeDescription(summary?.value || '');
                    seoDescription.value = suggestedDescription;
                }
                writeCount(seoDescriptionCounts, seoDescription.value.length, 170);
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

        const composePreviewTitle = form.querySelector('[data-compose-preview-title]');
        const composeTitle = form.querySelector('[data-compose-title]');
        composeTitle?.addEventListener('input', () => {
            if (composePreviewTitle) composePreviewTitle.textContent = composeTitle.value.trim() || 'Tiêu đề bài viết';
        });

        const composeCategory = form.querySelector('[data-compose-category]');
        const composeCategoryLabel = form.querySelector('[data-compose-category-label]');
        const renderCategory = () => {
            if (!composeCategory || !composeCategoryLabel) return;
            const option = composeCategory.selectedOptions[0];
            composeCategoryLabel.textContent = option?.textContent?.trim() || '—';
        };
        composeCategory?.addEventListener('change', renderCategory);
        renderCategory();
    });
})();
