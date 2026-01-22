// Theme toggle
document.querySelectorAll('.theme-toggle').forEach(function(btn) {
  btn.addEventListener('click', function() {
    const current = document.documentElement.dataset.theme || 'dark';
    const next = current === 'dark' ? 'light' : 'dark';
    document.documentElement.dataset.theme = next;
    localStorage.setItem('theme', next);
  });
});

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
