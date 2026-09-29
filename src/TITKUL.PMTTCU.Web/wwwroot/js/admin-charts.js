(() => {
  if (!window.Chart) return;

  const palette = ["#155e75", "#0f766e", "#2563eb", "#d97706", "#9333ea", "#dc2626", "#4d7c0f", "#475569"];

  document.querySelectorAll("canvas[data-chart]").forEach((canvas) => {
    try {
      const data = JSON.parse(canvas.dataset.chart);
      if (!Array.isArray(data.labels)) return;

      const doughnut = canvas.dataset.chartKind === "doughnut";
      const palette = ["#155e75", "#0f766e", "#2563eb", "#d97706", "#9333ea", "#dc2626", "#4d7c0f", "#475569"];
      const multiDatasets = Array.isArray(data.datasets);
      const datasets = multiDatasets
        ? data.datasets.filter((dataset) => Array.isArray(dataset.values) && dataset.values.length === data.labels.length).map((dataset, index) => ({
            label: dataset.label || `Nhóm ${index + 1}`,
            data: dataset.values.map((value) => Number(value) || 0),
            backgroundColor: palette[index % palette.length],
            borderColor: palette[index % palette.length],
            borderWidth: 1,
            borderRadius: 4,
          }))
        : Array.isArray(data.values) && data.labels.length === data.values.length ? [{
            label: canvas.dataset.chartLabel || "Số lượng",
            data: data.values.map((value) => Number(value) || 0),
            backgroundColor: doughnut ? (Array.isArray(data.colors) ? data.colors : palette) : "#155e75",
            borderColor: doughnut ? (Array.isArray(data.colors) ? data.colors : palette) : "#0f4c5c",
            borderWidth: 1,
            borderRadius: doughnut ? 0 : 4,
          }] : [];
      if (datasets.length === 0) return;
      new window.Chart(canvas, {
        type: doughnut ? "doughnut" : "bar",
        data: {
          labels: data.labels,
          datasets,
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          indexAxis: doughnut ? "x" : "y",
          plugins: { legend: { display: canvas.dataset.chartHideLegend !== "true" && (doughnut || datasets.length > 1), position: "bottom" } },
          scales: doughnut ? {} : { x: { beginAtZero: true, max: canvas.dataset.chartLabel?.includes("%") ? 100 : undefined } },
        },
      });
    } catch {
      canvas.insertAdjacentText("afterend", "Không thể hiển thị biểu đồ; số liệu chi tiết vẫn có trong bảng bên dưới.");
    }
  });
})();
