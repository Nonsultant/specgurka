document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll('.dropdown-toggle-background').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var targetId = btn.getAttribute('data-target');
            var content = document.getElementById(targetId);
            var chevron = btn.querySelector('.fa-chevron-right');
            if (content.style.display === "none" || content.style.display === "") {
                content.style.display = "block";
                if (chevron) chevron.classList.add("chevron-rotated");
            } else {
                content.style.display = "none";
                if (chevron) chevron.classList.remove("chevron-rotated");
            }
        });
    });
});