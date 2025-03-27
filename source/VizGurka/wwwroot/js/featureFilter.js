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
                const statusMatch = 
                    (filter === 'Passed' && statusSrc.includes('passed.svg')) ||
                    (filter === 'Failed' && statusSrc.includes('failed.svg')) ||
                    (filter === 'NotImplemented' && statusSrc.includes('pending.svg'));
                
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