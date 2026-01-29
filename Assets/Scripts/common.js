// Theme toggle (Auto/Light/Dark + cycle)
(function() {
  var themeButtons = Array.prototype.slice.call(document.querySelectorAll('.theme-toggle button[data-theme]'));
  var cycleButton = document.querySelector('.theme-cycle-btn');
  var themeOrder = ['auto', 'light', 'dark'];
  var themeIcons = {
    auto: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true" focusable="false"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/></svg>',
    light: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true" focusable="false"><circle cx="12" cy="12" r="5"/><path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/></svg>',
    dark: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true" focusable="false"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>'
  };

  function getTheme() {
    return document.documentElement.dataset.theme || localStorage.getItem('theme') || 'auto';
  }

  function setTheme(theme) {
    document.documentElement.dataset.theme = theme;
    localStorage.setItem('theme', theme);
    updateActiveTheme(theme);
    updateCycleButton(theme);
  }

  function updateActiveTheme(theme) {
    if (!themeButtons.length) return;
    themeButtons.forEach(function(btn) {
      btn.classList.toggle('active', btn.dataset.theme === theme);
    });
  }

  function getCycleTitle(theme) {
    if (theme === 'light') return 'Light mode (click to switch to dark)';
    if (theme === 'dark') return 'Dark mode (click to switch to auto)';
    return 'Auto mode (click to switch to light)';
  }

  function updateCycleButton(theme) {
    if (!cycleButton) return;
    cycleButton.innerHTML = themeIcons[theme] || themeIcons.auto;
    var title = getCycleTitle(theme);
    cycleButton.setAttribute('title', title);
    cycleButton.setAttribute('aria-label', title);
  }

  var currentTheme = getTheme();
  updateActiveTheme(currentTheme);
  updateCycleButton(currentTheme);

  themeButtons.forEach(function(btn) {
    btn.addEventListener('click', function() {
      var theme = this.dataset.theme;
      if (!theme) return;
      setTheme(theme);
    });
  });

  if (cycleButton) {
    cycleButton.addEventListener('click', function() {
      var theme = getTheme();
      var index = themeOrder.indexOf(theme);
      var next = themeOrder[(index + 1) % themeOrder.length];
      setTheme(next);
    });
  }
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

// Benchmark summary renderer
(function() {
  let benchSummaryPromise = null;
  function loadBenchmarkSummary() {
    if (!benchSummaryPromise) {
      benchSummaryPromise = fetch('/data/benchmark-summary.json', { cache: 'no-store' })
        .then(function(res) { return res.ok ? res.json() : null; })
        .catch(function() { return null; });
    }
    return benchSummaryPromise;
  }

  function pickBenchmarkSummary(data) {
    if (!data) return null;
    var order = [
      ['windows', 'quick'],
      ['windows', 'full'],
      ['linux', 'quick'],
      ['linux', 'full'],
      ['macos', 'quick'],
      ['macos', 'full']
    ];
    for (var i = 0; i < order.length; i++) {
      var os = order[i][0];
      var mode = order[i][1];
      var entry = data && data[os] && data[os][mode];
      if (entry && entry.summary && entry.summary.length) return entry;
    }
    var keys = Object.keys(data || {});
    for (var k = 0; k < keys.length; k++) {
      var osEntry = data[keys[k]] || {};
      var modes = Object.keys(osEntry || {});
      for (var m = 0; m < modes.length; m++) {
        var modeEntry = osEntry[modes[m]];
        if (modeEntry && modeEntry.summary && modeEntry.summary.length) return modeEntry;
      }
    }
    return null;
  }

  function appendVendorCell(td, vendor, delta) {
    if (!vendor) return;
    if (vendor.mean) {
      var mean = document.createElement('div');
      mean.textContent = vendor.mean;
      td.appendChild(mean);
    }
    if (vendor.allocated) {
      var alloc = document.createElement('div');
      alloc.className = 'bench-dim';
      alloc.textContent = vendor.allocated;
      td.appendChild(alloc);
    }
    if (delta) {
      var d = document.createElement('div');
      d.className = 'bench-delta';
      d.textContent = delta;
      td.appendChild(d);
    }
  }

  function renderBenchmarkSummary() {
    var container = document.querySelector('[data-benchmark-summary]');
    if (!container || container.dataset.loaded === 'true') return;

    loadBenchmarkSummary().then(function(data) {
      var entry = pickBenchmarkSummary(data);
      if (!entry || !entry.summary || !entry.summary.length) {
        container.textContent = 'Benchmark summary unavailable.';
        container.dataset.loaded = 'true';
        return;
      }

      var table = document.createElement('table');
      table.className = 'bench-table bench-summary-table';

      var thead = document.createElement('thead');
      thead.innerHTML = '<tr>' +
        '<th>Benchmark</th>' +
        '<th>Scenario</th>' +
        '<th>Fastest</th>' +
        '<th>CodeGlyphX</th>' +
        '<th>ZXing.Net</th>' +
        '<th>QRCoder</th>' +
        '<th>Barcoder</th>' +
        '<th>CodeGlyphX vs Fastest</th>' +
        '<th>Alloc vs Fastest</th>' +
        '<th>Rating</th>' +
        '</tr>';
      table.appendChild(thead);

      var tbody = document.createElement('tbody');
      entry.summary.forEach(function(item) {
        var row = document.createElement('tr');
        var vendors = item.vendors || {};
        var deltas = item.deltas || {};

        var cells = [
          item.benchmark || '',
          item.scenario || '',
          item.fastestVendor ? (item.fastestVendor + ' ' + (item.fastestMean || '')).trim() : (item.fastestMean || '')
        ];

        cells.forEach(function(text) {
          var td = document.createElement('td');
          td.textContent = text;
          row.appendChild(td);
        });

        var cgxTd = document.createElement('td');
        appendVendorCell(cgxTd, vendors['CodeGlyphX'], '');
        row.appendChild(cgxTd);

        var zxTd = document.createElement('td');
        appendVendorCell(zxTd, vendors['ZXing.Net'], deltas['ZXing.Net']);
        row.appendChild(zxTd);

        var qrcTd = document.createElement('td');
        appendVendorCell(qrcTd, vendors['QRCoder'], deltas['QRCoder']);
        row.appendChild(qrcTd);

        var barTd = document.createElement('td');
        appendVendorCell(barTd, vendors['Barcoder'], deltas['Barcoder']);
        row.appendChild(barTd);

        var ratioTd = document.createElement('td');
        ratioTd.textContent = item.codeGlyphXVsFastestText || '';
        row.appendChild(ratioTd);

        var allocTd = document.createElement('td');
        allocTd.textContent = item.codeGlyphXAllocVsFastestText || '';
        row.appendChild(allocTd);

        var ratingTd = document.createElement('td');
        ratingTd.textContent = item.rating || '';
        row.appendChild(ratingTd);

        tbody.appendChild(row);
      });
      table.appendChild(tbody);

      container.innerHTML = '';
      container.appendChild(table);
      container.dataset.loaded = 'true';
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', renderBenchmarkSummary);
  } else {
    renderBenchmarkSummary();
  }
})();
