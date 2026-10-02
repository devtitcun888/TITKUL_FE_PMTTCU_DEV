(() => {
  const page = document.querySelector(".cms-posts-page, .cms-list-page");
  if (!page) return;

  // ── Check-all / individual checkboxes ─────────────────────────────────────
  const checkAll = page.querySelector("[data-cms-posts-check-all]");
  const boxes = () => [...page.querySelectorAll('input[form="cms-posts-bulk"][name="ids"]')];
  checkAll?.addEventListener("change", () => {
    boxes().forEach((box) => { box.checked = checkAll.checked; });
  });
  page.addEventListener("change", (event) => {
    if (!(event.target instanceof HTMLInputElement) || event.target.name !== "ids") return;
    if (!checkAll) return;
    const all = boxes();
    checkAll.checked = all.length > 0 && all.every((box) => box.checked);
    checkAll.indeterminate = all.some((box) => box.checked) && !checkAll.checked;
  });

  // ── Page-size select → auto submit ────────────────────────────────────────
  page.querySelector("[data-cms-posts-pagesize]")?.addEventListener("change", (event) => {
    event.target.closest("form")?.submit();
  });

  // ── Auto-filter: selects & checkbox → submit toolbar form ─────────────────
  const toolbar = page.querySelector("form[data-cms-autofilter]");
  if (toolbar) {
    const filterToggle = toolbar.querySelector("[data-cms-filter-toggle]");
    filterToggle?.addEventListener("click", () => {
      const isOpen = filterToggle.getAttribute("aria-expanded") === "true";
      filterToggle.setAttribute("aria-expanded", String(!isOpen));
      toolbar.classList.toggle("is-filter-open", !isOpen);
    });

    // selects & checkboxes with data-cms-filter
    toolbar.querySelectorAll("[data-cms-filter]").forEach((el) => {
      el.addEventListener("change", () => toolbar.submit());
    });

    // search input: submit on Enter (native) or after 600ms debounce on input
    const searchInput = toolbar.querySelector('input[name="q"]');
    if (searchInput) {
      let debounceTimer;
      searchInput.addEventListener("input", () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => toolbar.submit(), 600);
      });
    }
  }

  // ── Row context menus: smart positioning ──────────────────────────────────
  const menus = [...page.querySelectorAll(".cms-posts-row-menu, .cms-posts-bulk")];
  const placePanel = (menu) => {
    const panel = menu.querySelector(".cms-posts-row-panel, .cms-posts-bulk-menu");
    if (!panel) return;
    if (!menu.open) {
      panel.style.position = "";
      panel.style.top = "";
      panel.style.right = "";
      panel.style.left = "";
      panel.style.bottom = "";
      return;
    }
    const rect = menu.getBoundingClientRect();
    const width = Math.max(panel.getBoundingClientRect().width, 220);
    const height = panel.getBoundingClientRect().height || 160;
    const openUp = rect.bottom + 8 + height > window.innerHeight - 12;
    panel.style.position = "fixed";
    panel.style.right = Math.max(12, window.innerWidth - rect.right) + "px";
    panel.style.left = "auto";
    if (openUp) {
      panel.style.top = "auto";
      panel.style.bottom = Math.max(12, window.innerHeight - rect.top + 6) + "px";
    } else {
      panel.style.top = (rect.bottom + 6) + "px";
      panel.style.bottom = "auto";
    }
    if (width > window.innerWidth - 24) panel.style.right = "12px";
  };
  menus.forEach((menu) => {
    menu.addEventListener("toggle", () => {
      if (menu.open) menus.forEach((other) => { if (other !== menu) other.open = false; });
      placePanel(menu);
    });
  });
  document.addEventListener("click", (event) => {
    if (event.target.closest(".cms-posts-row-menu, .cms-posts-bulk")) return;
    menus.forEach((menu) => { menu.open = false; });
  });
  window.addEventListener("resize", () => menus.forEach(placePanel));
})();
