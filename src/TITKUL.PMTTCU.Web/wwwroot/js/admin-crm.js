(() => {
    const modal = document.querySelector(".crm-modal, .education-modal");
    if (!modal) return;

    const closeLink = modal.querySelector(".crm-modal-close, .education-modal-close");
    const closeHref = modal.dataset.closeHref || closeLink?.getAttribute("href");
    const dismiss = () => {
        if (closeHref) window.location.assign(closeHref);
    };

    modal.addEventListener("click", (event) => {
        if (event.target.classList.contains("crm-modal-backdrop")
            || event.target.classList.contains("education-modal-backdrop")
            || event.target.closest("[data-crm-dismiss]")) {
            dismiss();
        }
    });
    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape") dismiss();
    }, { once: true });

    const first = modal.querySelector("input:not([readonly]):not([type=hidden]):not([type=checkbox]):not([type=radio]), textarea, select");
    first?.focus();
})();
