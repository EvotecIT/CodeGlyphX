// Showcase carousel functionality
document.querySelectorAll('.showcase-gallery').forEach(function(gallery) {
  var carouselId = gallery.dataset.carousel;
  var dataScript = document.querySelector('script.carousel-data[data-carousel="' + carouselId + '"]');
  if (!dataScript) return;

  var captions = JSON.parse(dataScript.textContent);
  var currentTheme = 'dark';
  var currentSlide = 0;

  var themeTabs = gallery.querySelectorAll('.showcase-gallery-tab');
  var slides = gallery.querySelectorAll('.showcase-carousel-slide');
  var prevBtn = gallery.querySelector('.showcase-carousel-nav.prev');
  var nextBtn = gallery.querySelector('.showcase-carousel-nav.next');
  var dots = gallery.querySelectorAll('.showcase-carousel-dot');
  var thumbContainers = gallery.querySelectorAll('.showcase-carousel-thumbs');
  var captionEl = gallery.querySelector('.showcase-carousel-caption');
  var counterEl = gallery.querySelector('.showcase-carousel-counter');

  function updateCarousel() {
    var themeCaptions = captions[currentTheme];
    var totalSlides = themeCaptions.length;

    // Update slides visibility
    slides.forEach(function(slide) {
      var isCurrentTheme = slide.dataset.theme === currentTheme;
      var isCurrentSlide = parseInt(slide.dataset.index) === currentSlide;
      slide.style.display = isCurrentTheme ? '' : 'none';
      slide.classList.toggle('active', isCurrentTheme && isCurrentSlide);
    });

    // Update dots
    dots.forEach(function(dot, idx) {
      dot.classList.toggle('active', idx === currentSlide);
    });

    // Update thumbnails
    thumbContainers.forEach(function(container) {
      var isCurrentTheme = container.dataset.themeContainer === currentTheme;
      container.style.display = isCurrentTheme ? '' : 'none';
      if (isCurrentTheme) {
        container.querySelectorAll('.showcase-carousel-thumb').forEach(function(thumb, idx) {
          thumb.classList.toggle('active', idx === currentSlide);
        });
      }
    });

    // Update caption and counter
    if (captionEl) captionEl.textContent = themeCaptions[currentSlide];
    if (counterEl) counterEl.textContent = (currentSlide + 1) + ' / ' + totalSlides;
  }

  function goToSlide(index) {
    var totalSlides = captions[currentTheme].length;
    currentSlide = ((index % totalSlides) + totalSlides) % totalSlides;
    updateCarousel();
  }

  // Theme tab clicks
  themeTabs.forEach(function(tab) {
    tab.addEventListener('click', function() {
      currentTheme = tab.dataset.theme;
      currentSlide = 0;
      themeTabs.forEach(function(t) { t.classList.remove('active'); });
      tab.classList.add('active');
      updateCarousel();
    });
  });

  // Navigation buttons
  if (prevBtn) prevBtn.addEventListener('click', function() { goToSlide(currentSlide - 1); });
  if (nextBtn) nextBtn.addEventListener('click', function() { goToSlide(currentSlide + 1); });

  // Dot clicks
  dots.forEach(function(dot) {
    dot.addEventListener('click', function() {
      goToSlide(parseInt(dot.dataset.index));
    });
  });

  // Thumbnail clicks
  thumbContainers.forEach(function(container) {
    container.querySelectorAll('.showcase-carousel-thumb').forEach(function(thumb) {
      thumb.addEventListener('click', function() {
        goToSlide(parseInt(thumb.dataset.index));
      });
    });
  });
});
