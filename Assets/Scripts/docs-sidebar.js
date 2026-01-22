// Docs sidebar toggle (static pages)
var docsToggle = document.querySelector('.docs-sidebar-toggle');
var docsSidebar = document.querySelector('.docs-sidebar');
var docsOverlay = document.querySelector('.docs-sidebar-overlay');

if (docsToggle && docsSidebar) {
  docsToggle.addEventListener('click', function() {
    docsSidebar.classList.toggle('sidebar-open');
    if (docsOverlay) { docsOverlay.classList.toggle('active'); }
  });
}

if (docsOverlay && docsSidebar) {
  docsOverlay.addEventListener('click', function() {
    docsSidebar.classList.remove('sidebar-open');
    docsOverlay.classList.remove('active');
  });
}
