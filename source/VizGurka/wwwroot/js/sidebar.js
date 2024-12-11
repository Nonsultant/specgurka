const sidebar = document.getElementById('sidebar');
const resizer = document.getElementById('resizer');
const searchBox = document.getElementById('feature-search');
const featureListItems = document.querySelectorAll('.feature-list > li');

//----Sidebar resizing----

let isResizing = false;
let startX = 0;
let startWidth = 0;

// Retrieve the sidebar width from localStorage
const savedWidth = localStorage.getItem('sidebarWidth');
if (savedWidth) {
  sidebar.style.width = `${savedWidth}px`;
}

resizer.addEventListener('mousedown', (e) => {
  isResizing = true;
  startX = e.clientX;
  startWidth = sidebar.offsetWidth;

  document.body.style.cursor = 'ew-resize';
  document.body.classList.add('dragging');
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
  document.body.classList.remove('dragging');
  document.removeEventListener('mousemove', resizeSidebar);
  document.removeEventListener('mouseup', stopResizing);

  // Save the sidebar width to localStorage
  localStorage.setItem('sidebarWidth', sidebar.offsetWidth);
}

//----Feature search----

document.addEventListener('DOMContentLoaded', () => {
  const savedQuery = localStorage.getItem('searchQuery');
  if (savedQuery) {
    searchBox.value = savedQuery;
    triggerSearch(savedQuery);
  }
});

searchBox.addEventListener('input', (e) => {
  const query = e.target.value.toLowerCase();
  localStorage.setItem('searchQuery', query);
  triggerSearch(query);
});

function triggerSearch(query) {
  if (query === '') {
    featureListItems.forEach(item => {
      item.style.display = 'block';
      item.style.background = '#FFFFFF';
    });
    return;
  }

  featureListItems.forEach(item => {
    const featureName = item.querySelector('h3').textContent.toLowerCase();
    if (featureName.includes(query)) {
      item.style.display = 'block';
      item.style.background = '#CAE9F5';
    } else {
      item.style.display = 'none';
      item.style.background = '#FFFFFF';
    }
  });
}

sidebar.addEventListener('scroll', () => {
  localStorage.setItem('sidebarScrollPosition', sidebar.scrollTop);
});

document.addEventListener('DOMContentLoaded', () => {
  const savedScrollPosition = localStorage.getItem('sidebarScrollPosition');
  if (savedScrollPosition) {
    sidebar.scrollTop = savedScrollPosition;
  }
});