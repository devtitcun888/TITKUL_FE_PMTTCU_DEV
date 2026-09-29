(() => {
  const search = document.querySelector("[data-program-search]");
  const status = document.querySelector("[data-program-status]");
  const rows = [...document.querySelectorAll("[data-program-row]")];
  if (search && status && rows.length > 0) {
    const normalize = value => (value || "").normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLocaleLowerCase("vi").trim();
    const update = () => {
      const term = normalize(search.value);
      const selectedStatus = status.value;
      let visible = 0;
      for (const row of rows) {
        const matches = (!term || normalize(row.dataset.search).includes(term))
          && (!selectedStatus || row.dataset.status === selectedStatus);
        row.hidden = !matches;
        if (matches) visible++;
      }
      const count = document.querySelector("[data-program-count]");
      if (count) count.textContent = String(visible);
      const empty = document.querySelector("[data-program-empty]");
      if (empty) empty.hidden = visible !== 0;
    };

    search.addEventListener("input", update);
    status.addEventListener("change", update);
  }

  const modal = document.querySelector(".education-modal");
  if (modal) {
    modal.querySelector("#program-name")?.focus();
    modal.addEventListener("click", event => {
      if (event.target.classList.contains("education-modal-backdrop")) {
        window.location.assign("/admin/chuong-trinh");
      }
    });
    document.addEventListener("keydown", event => {
      if (event.key === "Escape") window.location.assign("/admin/chuong-trinh");
    }, { once: true });
  }
})();
