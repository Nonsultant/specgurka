    document.addEventListener("DOMContentLoaded", function () {
        const urlParams = new URLSearchParams(window.location.search);
        const ruleName = urlParams.get('rule');
        const featureId = urlParams.get('featureId');
        if (ruleName && featureId) {
            const targetId = `${featureId}-${ruleName}`;
            const targetElement = document.getElementById(targetId);
            if (targetElement) {
                targetElement.scrollIntoView({ behavior: 'smooth', block: 'start', inline: 'nearest' });
            }
        }
    });