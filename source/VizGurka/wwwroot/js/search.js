function handleSearch(event) {
    event.preventDefault();
    
    var productName = document.getElementById('productName').value;
    var query = document.getElementById('navbar_search').value;
    
    var url = '/search/' + encodeURIComponent(productName);
    
    if (query) {
        url += '/' + encodeURIComponent(query);
    }
    
    window.location.href = url;
    return false;
}


document.addEventListener('DOMContentLoaded', function () {
    var searchInput = document.getElementById('navbar_search');

    searchInput.addEventListener('focus', function () {
        this.dataset.placeholder = this.placeholder;
        this.placeholder = '';
    });

    searchInput.addEventListener('blur', function () {
        this.placeholder = this.dataset.placeholder;
    });
});
