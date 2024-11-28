const sidebar = document.getElementById('sidebar');
const resizer = document.getElementById('resizer');

let isResizing = false;
let startX = 0;
let startWidth = 0;

resizer.addEventListener('mousedown', (e) => {
  isResizing = true;
  startX = e.clientX;
  startWidth = sidebar.offsetWidth;

  document.body.style.cursor = 'ew-resize';
  document.addEventListener('mousemove', resizeSidebar);
  document.addEventListener('mouseup', stopResizing);
});

function resizeSidebar(e) {
  if (isResizing) {
    const dx = e.clientX - startX;
    const newWidth = startWidth + dx;
    const minWidth = 0;
    const maxWidth = 600;

    if (newWidth >= minWidth && newWidth <= maxWidth) {
      sidebar.style.width = `${newWidth}px`;
    }
  }
}

function stopResizing() {
  isResizing = false;
  document.body.style.cursor = 'default';
  document.removeEventListener('mousemove', resizeSidebar);
  document.removeEventListener('mouseup', stopResizing);
}
