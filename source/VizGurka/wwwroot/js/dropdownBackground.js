document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll('.dropdown-toggle-background').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var targetId = btn.getAttribute('data-target');
            var content = document.getElementById(targetId);
            if (content.style.display === "none" || content.style.display === "") {
                content.style.display = "block";
            } else {
                content.style.display = "none";
            }
        });
    });
});
