document.addEventListener('DOMContentLoaded', function() {
  const triggers = document.querySelectorAll('.arrow-trigger');

  triggers.forEach((trigger, index) => {
    const content = trigger.parentElement.nextElementSibling;
    const isOpen = localStorage.getItem(`dropdown-${index}`) === 'true';
    if (isOpen) {
      content.classList.add('show');
      trigger.parentElement.querySelector('.arrow').classList.add('down');
      trigger.parentElement.querySelector('.arrow').classList.remove('right');
    }
  });

  triggers.forEach((trigger, index) => {
    trigger.addEventListener('click', function() {
      const content = this.parentElement.nextElementSibling;
      const isOpen = content.classList.toggle('show');
      const arrow = this.parentElement.querySelector('.arrow');
      arrow.classList.toggle('down', isOpen);
      arrow.classList.toggle('right', !isOpen);
      localStorage.setItem(`dropdown-${index}`, isOpen);
    });
  });
});