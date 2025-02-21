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