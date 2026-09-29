const CK = window.CKEDITOR;

if (CK?.ClassicEditor) {
    class CmsVideoUrl extends CK.Plugin {
        static get pluginName() { return 'CmsVideoUrl'; }

        init() {
            const editor = this.editor;
            editor.ui.componentFactory.add('cmsVideoUrl', locale => {
                const button = new CK.ButtonView(locale);
                button.set({ label: 'Nhúng video URL', withText: true, tooltip: true });
                button.on('execute', () => {
                    const value = window.prompt('Dán liên kết YouTube hoặc Facebook video (HTTPS):');
                    const url = allowedVideoUrl(value);
                    if (!url) {
                        if (value) window.alert('Liên kết chưa đúng định dạng YouTube/Facebook được hỗ trợ.');
                        return;
                    }

                    const escaped = url.replaceAll('&', '&amp;').replaceAll('"', '&quot;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
                    const view = editor.data.processor.toView(`<p><a class="cms-video" href="${escaped}">Video nhúng</a></p>`);
                    editor.model.insertContent(editor.data.toModel(view));
                });
                return button;
            });
        }
    }

    const csrf = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
    document.querySelectorAll('textarea[data-editor="cms"]').forEach(textarea => {
        const contentRequired = textarea.required;
        textarea.required = false;
        const validationMessage = textarea.closest('div')?.querySelector('[data-editor-validation]');
        CK.ClassicEditor.create(textarea, {
            licenseKey: 'GPL',
            language: 'vi',
            plugins: [
                CK.Essentials, CK.Autoformat, CK.BlockQuote, CK.Bold,
                CK.Heading, CK.Image, CK.ImageCaption, CK.ImageStyle, CK.ImageToolbar,
                CK.ImageUpload, CK.Italic, CK.Link, CK.List, CK.Paragraph,
                CK.PasteFromOffice, CK.SimpleUploadAdapter, CK.Table, CK.TableToolbar,
                CK.Undo, CK.GeneralHtmlSupport, CmsVideoUrl
            ],
            toolbar: [
                'undo', 'redo', '|', 'heading', '|', 'bold', 'italic', 'link', '|',
                'bulletedList', 'numberedList', 'blockQuote', '|', 'insertTable',
                'imageUpload', 'cmsVideoUrl'
            ],
            heading: {
                options: [
                    { model: 'paragraph', title: 'Đoạn văn', class: 'ck-heading_paragraph' },
                    { model: 'heading2', view: 'h2', title: 'Tiêu đề lớn', class: 'ck-heading_heading2' },
                    { model: 'heading3', view: 'h3', title: 'Tiêu đề vừa', class: 'ck-heading_heading3' },
                    { model: 'heading4', view: 'h4', title: 'Tiêu đề nhỏ', class: 'ck-heading_heading4' }
                ]
            },
            image: {
                toolbar: ['imageTextAlternative', '|', 'imageStyle:inline', 'imageStyle:block', 'imageStyle:side'],
                upload: { types: ['jpeg', 'png'] }
            },
            table: { contentToolbar: ['tableColumn', 'tableRow', 'mergeTableCells'] },
            simpleUpload: {
                uploadUrl: '/admin/cms/image-upload',
                headers: csrf ? { RequestVerificationToken: csrf } : {}
            },
            htmlSupport: {
                allow: [{
                    name: /^(p|h2|h3|h4|ul|ol|li|a|blockquote|figure|figcaption|img|table|thead|tbody|tr|th|td|br|hr|strong|em|u|span|div)$/,
                    attributes: ['class', 'href', 'src', 'alt', 'title', 'width', 'height', 'colspan', 'rowspan', 'scope']
                }]
            },
            placeholder: 'Soạn nội dung tại đây. Có thể dán nội dung từ Word.'
        }).then(editor => {
            const form = textarea.form;
            form?.addEventListener('submit', event => {
                const html = editor.getData();
                textarea.value = html;
                const text = html.replace(/<[^>]*>/g, '').replace(/&nbsp;|&#160;/gi, '').trim();
                const hasMedia = /<(img|table|hr)\b/i.test(html);
                if (contentRequired && !text && !hasMedia) {
                    event.preventDefault();
                    if (validationMessage) validationMessage.hidden = false;
                    editor.editing.view.focus();
                    return;
                }
                if (validationMessage) validationMessage.hidden = true;
            });
        }).catch(error => {
            console.error('Không khởi tạo được trình soạn nội dung.', error);
            textarea.insertAdjacentHTML('beforebegin', '<p class="alert" role="status">Trình soạn thảo chưa tải được. Bạn vẫn có thể nhập nội dung văn bản.</p>');
        });
    });
}

const coverUpload = document.querySelector('[data-cms-cover-upload]');
if (coverUpload) {
    const form = coverUpload.closest('form');
    const token = form?.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
    const keyField = form?.querySelector('#coverKey');
    const thumbnailKeyField = form?.querySelector('#thumbnailKey');
    const preview = form?.querySelector('[data-cover-preview]');
    const status = form?.querySelector('[data-cover-status]');

    coverUpload.addEventListener('change', async () => {
        const file = coverUpload.files?.[0];
        if (!file) return;
        if (!['image/png', 'image/jpeg'].includes(file.type) || file.size > 5 * 1024 * 1024) {
            if (status) status.textContent = 'Chọn ảnh PNG hoặc JPEG không quá 5 MiB.';
            coverUpload.value = '';
            return;
        }

        let variants;
        try {
            variants = await optimizeCoverImages(file);
        } catch {
            if (status) status.textContent = 'Không xử lý được ảnh bìa này. Hãy chọn ảnh PNG hoặc JPEG khác.';
            coverUpload.value = '';
            return;
        }

        const data = new FormData();
        data.append('upload', variants.coverFile);
        if (variants.thumbnailFile) data.append('thumbnail', variants.thumbnailFile);
        if (status) status.textContent = `Đang tải ảnh bìa${variants.coverFile.size < file.size ? ` (${formatImageSize(file.size)} → ${formatImageSize(variants.coverFile.size)})` : ''}…`;
        try {
            const response = await fetch('/admin/cms/image-upload', {
                method: 'POST', body: data,
                headers: token ? { RequestVerificationToken: token } : {},
                credentials: 'same-origin'
            });
            const result = await response.json();
            const imageUrl = result?.urls?.default;
            if (!response.ok || !imageUrl || !result.storageKey) throw new Error(result?.error?.message || 'Không tải được ảnh.');
            if (keyField) keyField.value = result.storageKey;
            if (thumbnailKeyField) thumbnailKeyField.value = result.thumbnailKey || result.storageKey;
            if (preview) { preview.src = imageUrl; preview.hidden = false; }
            if (status) status.textContent = `Đã cập nhật ảnh bìa${variants.coverFile.size < file.size ? ` (${formatImageSize(file.size)} → ${formatImageSize(variants.coverFile.size)})` : ''}; thumbnail ${variants.thumbnailFile ? '480 px' : 'dùng chung ảnh bìa'}. Lưu bài viết để áp dụng.`;
        } catch (error) {
            if (status) status.textContent = error instanceof Error ? error.message : 'Không tải được ảnh.';
        } finally {
            coverUpload.value = '';
        }
    });

    form?.querySelector('[data-cover-clear]')?.addEventListener('click', () => {
        if (keyField) keyField.value = '';
        if (thumbnailKeyField) thumbnailKeyField.value = '';
        if (preview) { preview.removeAttribute('src'); preview.hidden = true; }
        if (status) status.textContent = 'Ảnh đại diện sẽ được gỡ khi lưu bài viết.';
    });
}

async function optimizeCoverImages(file) {
    if (!('createImageBitmap' in window) || !HTMLCanvasElement.prototype.toBlob) return { coverFile: file, thumbnailFile: null };
    let bitmap;
    try { bitmap = await createImageBitmap(file); } catch { return { coverFile: file, thumbnailFile: null }; }
    try {
        const coverFile = await renderCoverVariant(file, bitmap, 1600, 0.84);
        const thumbnail = await renderCoverVariant(file, bitmap, 480, 0.78);
        return { coverFile, thumbnailFile: thumbnail === coverFile || thumbnail === file ? null : thumbnail };
    } finally {
        bitmap.close();
    }
}

async function renderCoverVariant(file, bitmap, maxEdge, quality) {
    const scale = Math.min(1, maxEdge / Math.max(bitmap.width, bitmap.height));
    if (scale === 1 && file.size <= 1024 * 1024) return file;
    const canvas = document.createElement('canvas');
    canvas.width = Math.max(1, Math.round(bitmap.width * scale));
    canvas.height = Math.max(1, Math.round(bitmap.height * scale));
    const context = canvas.getContext('2d', { alpha: file.type === 'image/png' });
    if (!context) return file;
    context.drawImage(bitmap, 0, 0, canvas.width, canvas.height);
    const blob = await new Promise(resolve => canvas.toBlob(resolve, file.type, file.type === 'image/jpeg' ? quality : undefined));
    if (!blob || blob.size > 5 * 1024 * 1024 || (blob.size >= file.size && scale === 1)) return file;
    const extension = file.type === 'image/png' ? '.png' : '.jpg';
    const name = file.name.replace(/\.[^.]+$/, '');
    return new File([blob], name + extension, { type: file.type, lastModified: file.lastModified });
}

function formatImageSize(bytes) {
    return bytes < 1024 * 1024 ? `${Math.max(1, Math.ceil(bytes / 1024))} KiB` : `${(bytes / (1024 * 1024)).toFixed(2)} MiB`;
}

function allowedVideoUrl(value) {
    if (!value) return null;
    let url;
    try { url = new URL(value.trim()); } catch { return null; }
    if (url.protocol !== 'https:') return null;
    const host = url.hostname.toLowerCase();
    if (['youtube.com', 'www.youtube.com', 'm.youtube.com', 'youtu.be', 'www.youtu.be'].includes(host)) {
        const id = host.endsWith('youtu.be')
            ? url.pathname.split('/').filter(Boolean)[0]
            : url.pathname === '/watch' ? url.searchParams.get('v')
                : /^\/(embed|shorts)\//.test(url.pathname) ? url.pathname.split('/')[2] : null;
        return id && /^[A-Za-z0-9_-]{11}$/.test(id) ? url.href : null;
    }
    if (['facebook.com', 'www.facebook.com', 'm.facebook.com', 'fb.watch'].includes(host)) {
        return host === 'fb.watch' || /\/(videos|reel)\//i.test(url.pathname) || url.pathname === '/watch' ? url.href : null;
    }
    return null;
}
