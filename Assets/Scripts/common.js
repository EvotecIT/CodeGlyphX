// Theme toggle (Auto/Light/Dark)
(function() {
  function updateActiveTheme(theme) {
    document.querySelectorAll('.theme-toggle button').forEach(function(btn) {
      btn.classList.toggle('active', btn.dataset.theme === theme);
    });
  }

  // Set initial active state
  var currentTheme = document.documentElement.dataset.theme || 'auto';
  updateActiveTheme(currentTheme);

  // Handle theme button clicks
  document.querySelectorAll('.theme-toggle button[data-theme]').forEach(function(btn) {
    btn.addEventListener('click', function() {
      var theme = this.dataset.theme;
      document.documentElement.dataset.theme = theme;
      localStorage.setItem('theme', theme);
      updateActiveTheme(theme);
    });
  });
})();

// Keyboard focus visibility (show focus ring only for keyboard navigation)
function enableKeyboardFocus() { document.body.classList.add('using-keyboard'); }
function disableKeyboardFocus() { document.body.classList.remove('using-keyboard'); }
globalThis.addEventListener('keydown', function(e) {
  if (e.key === 'Tab' || e.key === 'ArrowUp' || e.key === 'ArrowDown' || e.key === 'ArrowLeft' || e.key === 'ArrowRight') {
    enableKeyboardFocus();
  }
});
globalThis.addEventListener('mousedown', disableKeyboardFocus, true);
globalThis.addEventListener('touchstart', disableKeyboardFocus, true);

// Mobile nav toggle
const navToggle = document.getElementById('nav-toggle');
if (navToggle) {
  navToggle.addEventListener('change', function() {
    document.body.classList.toggle('nav-open', this.checked);
  });
}
