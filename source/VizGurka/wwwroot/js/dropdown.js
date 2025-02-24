document.addEventListener('DOMContentLoaded', function () {
    var dropdowns = document.querySelectorAll('.feature-dropdown');
    
    function saveState(nodeId, isOpen) {
        localStorage.setItem('treeNode-' + nodeId, isOpen ? 'open' : 'closed');
    }

    function loadState(nodeId) {
        return localStorage.getItem('treeNode-' + nodeId) === 'open';
    }

    function toggleDropdown(dropdown, content, arrow) {
        var isOpen = content.classList.toggle('show');
        arrow.classList.toggle('right', !isOpen);
        arrow.classList.toggle('down', isOpen);
        return isOpen;
    }

    // Initialize dropdowns
    dropdowns.forEach(function (dropdown) {
        var content = dropdown.nextElementSibling;
        var arrow = dropdown.querySelector('.arrow');
        var nodeId = content.getAttribute('data-node-id');

        // Restore state on load
        if (loadState(nodeId)) {
            content.classList.add('show');
            arrow.classList.remove('right');
            arrow.classList.add('down');
        }

        dropdown.addEventListener('click', function () {
            var isOpen = toggleDropdown(dropdown, content, arrow);
            saveState(nodeId, isOpen);
        });
    });

    // Expand parents of the currently selected feature
    var selectedFeature = document.querySelector('.feature_sidebar.selected');
    if (selectedFeature) {
        var parent = selectedFeature.closest('.dropdown-content');
        while (parent) {
            var parentDropdown = parent.previousElementSibling;
            var parentArrow = parentDropdown.querySelector('.arrow');
            toggleDropdown(parentDropdown, parent, parentArrow);
            saveState(parent.getAttribute('data-node-id'), true);
            parent = parentDropdown.closest('.dropdown-content');
        }
    }
});