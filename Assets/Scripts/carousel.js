// Showcase carousel functionality
document.querySelectorAll('.showcase-gallery').forEach(function(gallery) {
  const carouselId = gallery.dataset.carousel;
  const dataScript = document.querySelector('script.carousel-data[data-carousel="' + carouselId + '"]');
  if (!dataScript) return;

  const captions = JSON.parse(dataScript.textContent);
  let currentTheme = 'dark';
  let currentSlide = 0;

  const themeTabs = gallery.querySelectorAll('.showcase-gallery-tab');
  const slides = gallery.querySelectorAll('.showcase-carousel-slide');
  const prevBtn = gallery.querySelector('.showcase-carousel-nav.prev');
  const nextBtn = gallery.querySelector('.showcase-carousel-nav.next');
  const dots = gallery.querySelectorAll('.showcase-carousel-dot');
  const thumbContainers = gallery.querySelectorAll('.showcase-carousel-thumbs');
  const captionEl = gallery.querySelector('.showcase-carousel-caption');
  const counterEl = gallery.querySelector('.showcase-carousel-counter');

  function updateCarousel() {
    const themeCaptions = captions[currentTheme];
    const totalSlides = themeCaptions.length;

    // Update slides visibility
    slides.forEach(function(slide) {
      const isCurrentTheme = slide.dataset.theme === currentTheme;
      const isCurrentSlide = Number.parseInt(slide.dataset.index, 10) === currentSlide;
      slide.style.display = isCurrentTheme ? '' : 'none';
      slide.classList.toggle('active', isCurrentTheme && isCurrentSlide);
    });

    // Update dots
    dots.forEach(function(dot, idx) {
      dot.classList.toggle('active', idx === currentSlide);
    });

    // Update thumbnails
    thumbContainers.forEach(function(container) {
      const isCurrentTheme = container.dataset.themeContainer === currentTheme;
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
    const totalSlides = captions[currentTheme].length;
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
      goToSlide(Number.parseInt(dot.dataset.index, 10));
    });
  });

  // Thumbnail clicks
  thumbContainers.forEach(function(container) {
    container.querySelectorAll('.showcase-carousel-thumb').forEach(function(thumb) {
      thumb.addEventListener('click', function() {
        goToSlide(Number.parseInt(thumb.dataset.index, 10));
      });
    });
  });
});
