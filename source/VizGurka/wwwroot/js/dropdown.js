document.addEventListener('DOMContentLoaded', function () {
    var dropdowns = document.querySelectorAll('.dropdown-trigger');
    
    function saveState(nodeId, isOpen) {
        localStorage.setItem('treeNode-' + nodeId, isOpen ? 'open' : 'closed');
    }

    function loadState(nodeId) {
        return localStorage.getItem('treeNode-' + nodeId) === 'open';
    }

    function toggleDropdown(content) {
        var isOpen = content.classList.toggle('show');
        return isOpen;
    }

    // Initialize dropdowns
    dropdowns.forEach(function (trigger) {
        var content = trigger.parentElement.nextElementSibling;
        var nodeId = content.getAttribute('data-node-id');

        // Restore state on load
        if (loadState(nodeId)) {
            content.classList.add('show');
        }

        trigger.addEventListener('click', function () {
            var isOpen = toggleDropdown(content);
            saveState(nodeId, isOpen);
        });
    });

    // Expand parents of the currently selected feature
    var selectedFeature = document.querySelector('.feature_sidebar.selected');
    if (selectedFeature) {
        var parent = selectedFeature.closest('.dropdown-content');
        while (parent) {
            var parentTrigger = parent.previousElementSibling.querySelector('.dropdown-trigger');
            toggleDropdown(parent);
            saveState(parent.getAttribute('data-node-id'), true);
            parent = parentTrigger.closest('.dropdown-content');
        }
    }
});