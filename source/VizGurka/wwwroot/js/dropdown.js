document.addEventListener('DOMContentLoaded', function() {
  const dropdowns = document.querySelectorAll('.feature-dropdown');

  dropdowns.forEach((dropdown, index) => {
    const content = dropdown.parentElement.nextElementSibling;
    const isOpen = localStorage.getItem(`dropdown-${index}`) === 'true';
    if (isOpen) {
      content.classList.add('show');
      dropdown.querySelector('i').classList.add('down');
    }
  });

  dropdowns.forEach((dropdown, index) => {
    dropdown.addEventListener('click', function() {
      const content = this.parentElement.nextElementSibling;
      const isOpen = content.classList.toggle('show');
      this.querySelector('i').classList.toggle('down');
      localStorage.setItem(`dropdown-${index}`, isOpen);
    });
  });
});