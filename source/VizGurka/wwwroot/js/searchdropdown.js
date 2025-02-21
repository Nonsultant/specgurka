document.addEventListener('DOMContentLoaded', function() {
    var dropdownHeaders = document.querySelectorAll('.dropdown-header');

    dropdownHeaders.forEach(function(header) {
        header.addEventListener('click', function() {
            var content = this.nextElementSibling;
            var arrow = this.querySelector('.arrow');

            if (content.classList.contains('show')) {
                content.classList.remove('show');
                arrow.classList.remove('down');
                arrow.classList.add('right');
            } else {
                content.classList.add('show');
                arrow.classList.remove('right');
                arrow.classList.add('down');
            }
        });
    });
});