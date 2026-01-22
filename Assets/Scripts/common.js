// Theme toggle
document.querySelectorAll('.theme-toggle').forEach(function(btn) {
  btn.addEventListener('click', function() {
    var current = document.documentElement.getAttribute('data-theme') || 'dark';
    var next = current === 'dark' ? 'light' : 'dark';
    document.documentElement.setAttribute('data-theme', next);
    localStorage.setItem('theme', next);
  });
});

// Keyboard focus visibility (show focus ring only for keyboard navigation)
function enableKeyboardFocus() { document.body.classList.add('using-keyboard'); }
function disableKeyboardFocus() { document.body.classList.remove('using-keyboard'); }
window.addEventListener('keydown', function(e) {
  if (e.key === 'Tab' || e.key === 'ArrowUp' || e.key === 'ArrowDown' || e.key === 'ArrowLeft' || e.key === 'ArrowRight') {
    enableKeyboardFocus();
  }
});
window.addEventListener('mousedown', disableKeyboardFocus, true);
window.addEventListener('touchstart', disableKeyboardFocus, true);

// Mobile nav toggle
var navToggle = document.getElementById('nav-toggle');
if (navToggle) {
  navToggle.addEventListener('change', function() {
    document.body.classList.toggle('nav-open', this.checked);
  });
}
