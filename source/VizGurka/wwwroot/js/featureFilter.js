document.addEventListener('DOMContentLoaded', function() {
    const filterButtons = document.querySelectorAll('.filter-option');

    filterButtons.forEach(button => {
        button.addEventListener('click', function() {
            filterButtons.forEach(btn => btn.classList.remove('active'));

            this.classList.add('active');

            const filter = this.getAttribute('data-filter');

            applyFeatureFilter(filter);
        });
    });

    function applyFeatureFilter(filter) {
        const features = document.querySelectorAll('.feature_sidebar');
        const treeNodes = document.querySelectorAll('li[id^="tree-node-"]');

        if (filter === 'All') {
            features.forEach(feature => {
                const parentLi = feature.closest('li');
                if (parentLi) parentLi.style.display = 'block';
            });

            treeNodes.forEach(node => {
                node.style.display = 'block';
            });
            return;
        }

        features.forEach(feature => {
            const parentLi = feature.closest('li');
            if (parentLi) parentLi.style.display = 'none';
        });

        features.forEach(feature => {
            const statusImg = feature.parentElement.querySelector('.status_img');
            if (statusImg) {
                const statusSrc = statusImg.getAttribute('src');
                let statusMatch = false;
                if (filter === 'Passed') {
                    statusMatch = statusSrc.includes('passed.svg');
                } else if (filter === 'Failed') {
                    statusMatch = statusSrc.includes('failed.svg');
                } else if (filter === 'NotImplemented') {
                    statusMatch = statusSrc.includes('pending.svg') || statusSrc.includes('draft.svg');
                } else if (filter == 'Draft') {
                    statusMatch = statusSrc.includes('draft.svg');
                }
                
                if (statusMatch) {
                    const parentLi = feature.closest('li');
                    if (parentLi) parentLi.style.display = 'block';
                }
            }
        });

        treeNodes.forEach(node => {
            const dropdownContent = node.querySelector('.dropdown-content');
            if (dropdownContent) {
                const visibleFeatures = dropdownContent.querySelectorAll('li[style="display: block;"]');
                node.style.display = visibleFeatures.length > 0 ? 'block' : 'none';
            }
        });
    }
});