(() => {
  const forms = document.querySelectorAll('form[data-survey-branch]');
  for (const form of forms) {
    const questions = [...form.querySelectorAll("fieldset[data-question-order]")];
    const refresh = () => {
      questions.forEach((question) => {
        question.hidden = false;
        question.disabled = false;
      });
      let index = 0;
      const visible = new Set();
      while (index < questions.length && !visible.has(index)) {
        visible.add(index);
        const question = questions[index];
        const selected = question.querySelector('input[type="radio"]:checked[data-jump-to-order]');
        const target = Number(selected?.dataset.jumpToOrder);
        const next = target ? questions.findIndex((item) => Number(item.dataset.questionOrder) === target) : -1;
        index = next > index ? next : index + 1;
      }
      questions.forEach((question, questionIndex) => {
        if (visible.has(questionIndex)) return;
        question.hidden = true;
        question.disabled = true;
      });
    };
    form.addEventListener("change", refresh);
    refresh();
  }
})();
