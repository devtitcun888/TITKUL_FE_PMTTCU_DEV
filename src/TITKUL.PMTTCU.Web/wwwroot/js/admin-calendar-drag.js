(() => {
  let draggedId = null;
  document.querySelectorAll(".month-session[draggable='true']").forEach((session) => {
    session.addEventListener("dragstart", (event) => {
      draggedId = session.dataset.sessionId;
      event.dataTransfer?.setData("text/plain", draggedId || "");
      if (event.dataTransfer) event.dataTransfer.effectAllowed = "move";
    });
  });
  document.querySelectorAll("[data-calendar-day]").forEach((day) => {
    day.addEventListener("dragover", (event) => event.preventDefault());
    day.addEventListener("drop", async (event) => {
      event.preventDefault();
      const id = event.dataTransfer?.getData("text/plain") || draggedId;
      const form = id ? document.querySelector(`form[data-session-form='${CSS.escape(id)}']`) : null;
      if (!form) return;
      form.querySelector("[name='targetDay']").value = day.dataset.calendarDay;
      const response = await fetch(form.action || window.location.href, {
        method: "POST",
        body: new FormData(form),
        headers: { "X-Requested-With": "XMLHttpRequest" },
        credentials: "same-origin"
      });
      if (response.ok) window.location.reload();
      else window.alert("Không chuyển được buổi học. Có thể thời gian mới bị trùng hoặc bạn không có quyền sửa lịch.");
      draggedId = null;
    });
  });
})();
