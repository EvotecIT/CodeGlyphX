document.querySelectorAll('[data-style-board]').forEach(function (board) {
  const src = board.dataset.styleBoardSrc || '/data/style-board.json';
  const base = board.dataset.styleBoardBase || '/assets/style-board/';

  function addCard(item) {
    const card = document.createElement('div');
    card.className = 'style-board-card';

    const img = document.createElement('img');
    img.loading = 'lazy';
    img.alt = item.name || 'QR style';
    img.src = base + item.file;
    card.appendChild(img);

    const label = document.createElement('div');
    label.className = 'style-board-label';
    label.textContent = item.name || 'QR Style';
    card.appendChild(label);

    board.appendChild(card);
  }

  fetch(src)
    .then(function (res) {
      if (!res.ok) throw new Error('Failed to load style board');
      return res.json();
    })
    .then(function (items) {
      if (!Array.isArray(items)) return;
      board.innerHTML = '';
      items.forEach(addCard);
    })
    .catch(function () {
      board.innerHTML = '<div class="style-board-fallback">Style board assets not available yet.</div>';
    });
});
