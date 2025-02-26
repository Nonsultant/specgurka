document.addEventListener('DOMContentLoaded', function () {
    const dropdownTriggers = document.querySelectorAll('.dropdown-trigger');
    
    function saveState(nodeId, isOpen) {
        localStorage.setItem('treeNode-' + nodeId, isOpen ? 'open' : 'closed');
    }

    function loadState(nodeId) {
        return localStorage.getItem('treeNode-' + nodeId) === 'open';
    }

    // Initialize all dropdown triggers
    dropdownTriggers.forEach(function (trigger) {
        const nodeId = trigger.getAttribute('data-node-id');
        const content = document.querySelector(`.dropdown-content[data-node-id="${nodeId}"]`);
        
        if (!content) return;
        
        // Restore state on load
        if (loadState(nodeId)) {
            content.classList.add('show');
            
            const arrows = document.querySelectorAll(`.arrow-icon[data-node-id="${nodeId}"]`);
            arrows.forEach(arrow => {
                arrow.classList.add('rotated');
            });
        }

        trigger.addEventListener('click', function (e) {
            if (this.tagName === 'A' && e.target === this) {
                return true;
            }
            
            e.preventDefault();
            
            const isOpen = content.classList.toggle('show');
            
            const arrows = document.querySelectorAll(`.arrow-icon[data-node-id="${nodeId}"]`);
            arrows.forEach(arrow => {
                if (isOpen) {
                    arrow.classList.add('rotated');
                } else {
                    arrow.classList.remove('rotated');
                }
            });

            saveState(nodeId, isOpen);
            
            e.stopPropagation();
        });
    });

    const activeItem = document.querySelector('.active');
    if (activeItem) {
        let parent = activeItem.closest('.dropdown-content');
        while (parent) {
            parent.classList.add('show');
            
            const nodeId = parent.getAttribute('data-node-id');
            if (nodeId) {
                const arrows = document.querySelectorAll(`.arrow-icon[data-node-id="${nodeId}"]`);
                arrows.forEach(arrow => {
                    arrow.classList.add('rotated');
                });
                saveState(nodeId, true);
            }
            
            parent = parent.parentElement.closest('.dropdown-content');
        }
    }
});