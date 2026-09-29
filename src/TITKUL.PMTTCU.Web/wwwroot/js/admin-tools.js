document.addEventListener('click', async (event) => {
    const button = event.target.closest('[data-copy-url]');
    if (!button) return;

    const url = new URL(button.dataset.copyUrl, window.location.origin).toString();
    const feedback = button.closest('section')?.querySelector('[data-copy-feedback]');
    try {
        await navigator.clipboard.writeText(url);
        if (feedback) feedback.textContent = 'Đã sao chép liên kết: ' + url;
    } catch {
        if (feedback) feedback.textContent = 'Không thể sao chép tự động. Liên kết: ' + url;
    }
});

document.addEventListener('change', (event) => {
    const input = event.target.closest('input[type="file"][data-file-preview]');
    if (!input) return;

    const file = input.files?.[0];
    const isImage = /\.(png|jpe?g)$/i.test(file?.name ?? '');
    const maxBytes = isImage && input.dataset.imageMax
        ? Number(input.dataset.imageMax)
        : Number(input.dataset.filePreview);
    const status = input.parentElement.querySelector('[data-file-preview-status]');
    if (!file) {
        input.setCustomValidity('');
        if (status) status.textContent = '';
        return;
    }

    const size = file.size < 1024 * 1024
        ? `${Math.max(1, Math.ceil(file.size / 1024))} KiB`
        : `${(file.size / (1024 * 1024)).toFixed(2)} MiB`;
    const tooLarge = file.size > maxBytes;
    input.setCustomValidity(tooLarge ? `Tệp vượt quá giới hạn ${Math.round(maxBytes / (1024 * 1024))} MiB.` : '');
    if (status) status.textContent = tooLarge
        ? `${file.name} · ${size} — vượt giới hạn cho phép.`
        : `${file.name} · ${size}`;
});
