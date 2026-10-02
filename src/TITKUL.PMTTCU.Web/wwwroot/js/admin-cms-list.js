(() => {
  const page = document.querySelector(".cms-posts-page, .cms-list-page");
  if (!page) return;

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

  page.querySelector("[data-cms-posts-pagesize]")?.addEventListener("change", (event) => {
    event.target.closest("form")?.submit();
  });

  page.querySelectorAll("[data-cms-autosubmit]").forEach((el) => {
    el.addEventListener("change", () => el.closest("form")?.submit());
  });

  const toolbar = page.querySelector("form[data-cms-autofilter]");
  if (toolbar) {
    const filterToggle = toolbar.querySelector("[data-cms-filter-toggle]");
    filterToggle?.addEventListener("click", () => {
      const isOpen = filterToggle.getAttribute("aria-expanded") === "true";
      filterToggle.setAttribute("aria-expanded", String(!isOpen));
      toolbar.classList.toggle("is-filter-open", !isOpen);
    });

    toolbar.querySelectorAll("[data-cms-filter]").forEach((el) => {
      el.addEventListener("change", () => toolbar.submit());
    });

    toolbar.querySelectorAll('input[type="search"], input[type="text"], input[type="number"], input:not([type])').forEach((searchInput) => {
      let debounceTimer;
      searchInput.addEventListener("input", () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => toolbar.submit(), 600);
      });
    });
  }

  const menus = [...page.querySelectorAll(".cms-posts-row-menu, .cms-posts-bulk, .cms-list-row-menu")];
  const placePanel = (menu) => {
    const panel = menu.querySelector(".cms-posts-row-panel, .cms-posts-bulk-menu, .cms-list-row-panel");
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
    if (event.target.closest(".cms-posts-row-menu, .cms-posts-bulk, .cms-list-row-menu")) return;
    menus.forEach((menu) => { menu.open = false; });
  });
  window.addEventListener("resize", () => menus.forEach(placePanel));
})();
