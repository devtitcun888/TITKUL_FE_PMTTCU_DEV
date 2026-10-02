(() => {
  const root = document.querySelector("[data-feature-carousel]");
  if (!root) return;

  const slides = [...root.querySelectorAll("[data-feature-slide]")];
  const controls = root.querySelector("[data-feature-controls]");
  const previous = root.querySelector("[data-feature-previous]");
  const next = root.querySelector("[data-feature-next]");
  const toggle = root.querySelector("[data-feature-toggle]");
  const position = root.querySelector("[data-feature-position]");
  const thumbs = [...root.querySelectorAll("[data-feature-thumb]")];
  if (slides.length < 2 || !controls || !previous || !next || !toggle || !position) return;

  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
  const configuredInterval = Number.parseInt(root.dataset.interval || "7000", 10);
  const interval = Number.isFinite(configuredInterval)
    ? Math.min(15000, Math.max(4000, configuredInterval))
    : 7000;
  let current = 0;
  let timer = 0;
  let hovered = false;
  let focused = false;
  let paused = reducedMotion.matches;

  const render = (index, animate = true) => {
    current = (index + slides.length) % slides.length;
    slides.forEach((slide, slideIndex) => {
      const active = slideIndex === current;
      slide.hidden = !active;
      slide.setAttribute("aria-hidden", String(!active));
      if (!active) slide.setAttribute("tabindex", "-1");
      else slide.removeAttribute("tabindex");
      slide.classList.remove("is-entering");
    });
    thumbs.forEach((thumb, thumbIndex) => {
      if (thumbIndex === current) thumb.setAttribute("aria-current", "true");
      else thumb.removeAttribute("aria-current");
    });
    position.textContent = `${current + 1} / ${slides.length}`;
    if (animate && !reducedMotion.matches) {
      window.requestAnimationFrame(() => slides[current]?.classList.add("is-entering"));
    }
  };

  const renderToggle = () => {
    const icon = toggle.querySelector(".material-symbols-outlined");
    const label = toggle.querySelector(".portal-feature-toggle-label");
    toggle.setAttribute("aria-pressed", String(paused));
    toggle.setAttribute("aria-label", paused ? "Bật tự chuyển tin nổi bật" : "Tạm dừng tự chuyển tin nổi bật");
    if (icon) icon.textContent = paused ? "play_arrow" : "pause";
    if (label) label.textContent = paused ? "Tự chạy: Tắt" : "Tạm dừng";
  };

  const clearTimer = () => {
    if (timer) window.clearTimeout(timer);
    timer = 0;
  };

  const schedule = () => {
    clearTimer();
    if (paused || hovered || focused || document.hidden || reducedMotion.matches) return;
    timer = window.setTimeout(() => {
      render(current + 1);
      schedule();
    }, interval);
  };

  const moveTo = (index) => {
    paused = true;
    render(index);
    renderToggle();
    schedule();
  };

  previous.addEventListener("click", () => moveTo(current - 1));
  next.addEventListener("click", () => moveTo(current + 1));
  thumbs.forEach((thumb) => {
    thumb.addEventListener("click", () => {
      const index = Number.parseInt(thumb.dataset.featureIndex || "", 10);
      if (Number.isInteger(index)) moveTo(index);
    });
  });
  toggle.addEventListener("click", () => {
    paused = !paused;
    renderToggle();
    schedule();
  });
  root.addEventListener("mouseenter", () => { hovered = true; clearTimer(); });
  root.addEventListener("mouseleave", () => { hovered = false; schedule(); });
  root.addEventListener("focusin", () => { focused = true; clearTimer(); });
  root.addEventListener("focusout", (event) => {
    if (!root.contains(event.relatedTarget)) { focused = false; schedule(); }
  });
  document.addEventListener("visibilitychange", schedule);
  reducedMotion.addEventListener?.("change", () => {
    if (reducedMotion.matches) paused = true;
    renderToggle();
    schedule();
  });

  render(0, false);
  renderToggle();
  controls.hidden = false;
  schedule();
})();
