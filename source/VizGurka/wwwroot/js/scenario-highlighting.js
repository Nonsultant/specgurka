document.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('.scenario').forEach(scenario => {
    scenario.addEventListener('click', () => {
      window.location.hash = scenario.id;
    });
  });
});