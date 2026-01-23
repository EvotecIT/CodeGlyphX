// Benchmark page renderer
(function() {
  var currentMode = 'quick';
  var currentOs = 'windows';
  var summaryData = null;
  var detailData = null;

  function loadJson(url) {
    return fetch(url, { cache: 'no-store' })
      .then(function(res) { return res.ok ? res.json() : null; })
      .catch(function() { return null; });
  }

  function escapeHtml(text) {
    if (!text) return '';
    return String(text).replace(/[&<>"']/g, function(m) {
      return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[m];
    });
  }

  function getEntry(data, os, mode) {
    return data && data[os] && data[os][mode] ? data[os][mode] : null;
  }

  function findBestEntry(data) {
    // Priority: windows full > windows quick > linux full > linux quick > macos
    var order = [
      ['windows', 'full'], ['windows', 'quick'],
      ['linux', 'full'], ['linux', 'quick'],
      ['macos', 'full'], ['macos', 'quick']
    ];
    for (var i = 0; i < order.length; i++) {
      var entry = getEntry(data, order[i][0], order[i][1]);
      if (entry && (entry.summary && entry.summary.length || entry.comparisons && entry.comparisons.length)) {
        currentOs = order[i][0];
        currentMode = order[i][1];
        return entry;
      }
    }
    return null;
  }

  function formatDate(isoString) {
    if (!isoString) return 'Unknown';
    try {
      var d = new Date(isoString);
      return d.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
    } catch (e) {
      return isoString;
    }
  }

  function renderMeta(entry) {
    var container = document.querySelector('[data-benchmark-meta]');
    if (!container) return;

    if (!entry) {
      container.innerHTML = '<p class="benchmark-warning">No benchmark data available.</p>';
      return;
    }

    var meta = entry.meta || {};
    var html = '<div class="benchmark-meta-grid">';
    html += '<div class="meta-item"><span class="meta-label">Last Updated</span><span class="meta-value">' + escapeHtml(formatDate(entry.generatedUtc)) + '</span></div>';
    html += '<div class="meta-item"><span class="meta-label">Mode</span><span class="meta-value">' + escapeHtml(entry.runMode || 'unknown') + '</span></div>';
    html += '<div class="meta-item"><span class="meta-label">OS</span><span class="meta-value">' + escapeHtml(entry.os || 'unknown') + '</span></div>';
    html += '<div class="meta-item"><span class="meta-label">Framework</span><span class="meta-value">' + escapeHtml(entry.framework || 'unknown') + '</span></div>';
    html += '</div>';
    container.innerHTML = html;
  }

  function renderModeSelector(summaryData, detailData) {
    var buttons = document.querySelectorAll('.benchmark-mode-btn');
    var noteEl = document.querySelector('[data-mode-note]');

    buttons.forEach(function(btn) {
      var mode = btn.dataset.mode;
      var summaryEntry = getEntry(summaryData, currentOs, mode);
      var detailEntry = getEntry(detailData, currentOs, mode);
      var hasData = (summaryEntry && summaryEntry.summary && summaryEntry.summary.length) ||
                    (detailEntry && detailEntry.comparisons && detailEntry.comparisons.length);

      btn.disabled = !hasData;
      btn.classList.toggle('active', mode === currentMode);

      if (!hasData) {
        btn.title = 'No ' + mode + ' benchmark data available yet';
      } else {
        btn.title = '';
      }

      btn.onclick = function() {
        if (this.disabled) return;
        currentMode = mode;
        buttons.forEach(function(b) { b.classList.toggle('active', b.dataset.mode === mode); });
        renderAll();
      };
    });

    // Update note
    if (noteEl) {
      var entry = getEntry(summaryData, currentOs, currentMode);
      if (entry && entry.runModeDetails) {
        noteEl.textContent = entry.runModeDetails;
      } else if (currentMode === 'quick') {
        noteEl.textContent = 'Quick mode: Fewer iterations, higher variance. Use for rough estimates only.';
      } else {
        noteEl.textContent = 'Full mode: BenchmarkDotNet defaults with statistical analysis.';
      }
    }
  }

  function renderSummaryTable(entry) {
    var container = document.querySelector('[data-benchmark-summary]');
    if (!container) return;

    if (!entry || !entry.summary || !entry.summary.length) {
      container.innerHTML = '<p class="benchmark-no-data">No comparison summary available for this mode.</p>';
      return;
    }

    var vendors = ['CodeGlyphX', 'ZXing.Net', 'QRCoder', 'Barcoder'];
    var html = '<div class="table-scroll"><table class="bench-table">';
    html += '<thead><tr>';
    html += '<th>Scenario</th>';
    html += '<th>Fastest</th>';
    vendors.forEach(function(v) {
      html += '<th>' + escapeHtml(v) + '</th>';
    });
    html += '<th>Rating</th>';
    html += '</tr></thead><tbody>';

    entry.summary.forEach(function(item) {
      html += '<tr>';
      html += '<td>' + escapeHtml(item.scenario || item.benchmark || '') + '</td>';
      html += '<td class="bench-fastest">' + escapeHtml(item.fastestVendor || '') + '</td>';

      vendors.forEach(function(v) {
        var vendor = item.vendors && item.vendors[v];
        if (vendor && vendor.mean) {
          var isFastest = item.fastestVendor === v;
          html += '<td class="' + (isFastest ? 'bench-winner' : '') + '">';
          html += '<div>' + escapeHtml(vendor.mean) + '</div>';
          if (vendor.allocated) {
            html += '<div class="bench-dim">' + escapeHtml(vendor.allocated) + '</div>';
          }
          html += '</td>';
        } else {
          html += '<td class="bench-na">-</td>';
        }
      });

      var ratingClass = 'bench-rating-' + (item.rating || 'unknown');
      html += '<td class="' + ratingClass + '">' + escapeHtml(item.rating || '-') + '</td>';
      html += '</tr>';
    });

    html += '</tbody></table></div>';

    // Add legend
    html += '<div class="bench-legend">';
    html += '<span class="bench-legend-item"><span class="bench-rating-good">good</span> = within 10% time, 25% allocation</span>';
    html += '<span class="bench-legend-item"><span class="bench-rating-ok">ok</span> = within 50% time, 100% allocation</span>';
    html += '<span class="bench-legend-item"><span class="bench-rating-bad">bad</span> = outside these bounds</span>';
    html += '</div>';

    container.innerHTML = html;
  }

  function renderDetails(entry) {
    var container = document.querySelector('[data-benchmark-details]');
    if (!container) return;

    if (!entry || !entry.comparisons || !entry.comparisons.length) {
      container.innerHTML = '<p class="benchmark-no-data">No detailed comparison data available for this mode.</p>';
      return;
    }

    var html = '';
    entry.comparisons.forEach(function(comp) {
      html += '<div class="benchmark-detail-section">';
      html += '<h3>' + escapeHtml(comp.title) + '</h3>';

      if (!comp.scenarios || !comp.scenarios.length) {
        html += '<p class="bench-na">No scenarios</p>';
        html += '</div>';
        return;
      }

      html += '<div class="table-scroll"><table class="bench-table bench-detail-table">';
      html += '<thead><tr><th>Scenario</th>';

      // Collect all vendors from all scenarios
      var allVendors = {};
      comp.scenarios.forEach(function(s) {
        if (s.vendors) {
          Object.keys(s.vendors).forEach(function(v) { allVendors[v] = true; });
        }
      });
      var vendorList = Object.keys(allVendors).sort(function(a, b) {
        if (a === 'CodeGlyphX') return -1;
        if (b === 'CodeGlyphX') return 1;
        return a.localeCompare(b);
      });

      vendorList.forEach(function(v) {
        html += '<th>' + escapeHtml(v) + '</th>';
      });
      html += '</tr></thead><tbody>';

      comp.scenarios.forEach(function(scenario) {
        html += '<tr>';
        html += '<td>' + escapeHtml(scenario.name) + '</td>';

        vendorList.forEach(function(v) {
          var vendor = scenario.vendors && scenario.vendors[v];
          if (vendor && vendor.mean) {
            html += '<td>';
            html += '<div>' + escapeHtml(vendor.mean) + '</div>';
            if (vendor.allocated) {
              html += '<div class="bench-dim">' + escapeHtml(vendor.allocated) + '</div>';
            }
            html += '</td>';
          } else {
            html += '<td class="bench-na">-</td>';
          }
        });
        html += '</tr>';
      });

      html += '</tbody></table></div>';
      html += '</div>';
    });

    container.innerHTML = html;
  }

  function renderBaseline(entry) {
    var container = document.querySelector('[data-benchmark-baseline]');
    if (!container) return;

    if (!entry || !entry.baselines || !entry.baselines.length) {
      container.innerHTML = '<p class="benchmark-no-data">No baseline data available for this mode.</p>';
      return;
    }

    var html = '';
    entry.baselines.forEach(function(baseline) {
      html += '<div class="benchmark-detail-section">';
      html += '<h3>' + escapeHtml(baseline.title) + '</h3>';

      if (!baseline.rows || !baseline.rows.length) {
        html += '<p class="bench-na">No data</p>';
        html += '</div>';
        return;
      }

      html += '<div class="table-scroll"><table class="bench-table bench-baseline-table">';
      html += '<thead><tr><th>Scenario</th><th>Mean</th><th>Allocated</th></tr></thead>';
      html += '<tbody>';

      baseline.rows.forEach(function(row) {
        html += '<tr>';
        html += '<td>' + escapeHtml(row.scenario) + '</td>';
        html += '<td>' + escapeHtml(row.mean || '-') + '</td>';
        html += '<td>' + escapeHtml(row.allocated || '-') + '</td>';
        html += '</tr>';
      });

      html += '</tbody></table></div>';
      html += '</div>';
    });

    container.innerHTML = html;
  }

  function renderEnvironment(entry) {
    var container = document.querySelector('[data-benchmark-environment]');
    if (!container) return;

    if (!entry || !entry.meta) {
      container.innerHTML = '<p class="benchmark-no-data">No environment info available.</p>';
      return;
    }

    var meta = entry.meta;
    var html = '<div class="benchmark-env-grid">';

    var items = [
      ['Machine', meta.machineName],
      ['OS', meta.osDescription],
      ['Architecture', meta.osArchitecture || meta.processArchitecture],
      ['.NET SDK', meta.dotnetSdk],
      ['Runtime', meta.runtime],
      ['Processors', meta.processorCount]
    ];

    items.forEach(function(item) {
      if (item[1]) {
        html += '<div class="env-item"><span class="env-label">' + escapeHtml(item[0]) + '</span>';
        html += '<span class="env-value">' + escapeHtml(item[1]) + '</span></div>';
      }
    });

    html += '</div>';
    container.innerHTML = html;
  }

  function renderNotes(entry) {
    var container = document.querySelector('[data-benchmark-notes]');
    if (!container || !entry || !entry.notes || !entry.notes.length) return;

    var html = '<ul>';
    entry.notes.forEach(function(note) {
      html += '<li>' + escapeHtml(note) + '</li>';
    });
    html += '</ul>';
    container.innerHTML = html;
  }

  function renderAll() {
    var summaryEntry = getEntry(summaryData, currentOs, currentMode);
    var detailEntry = getEntry(detailData, currentOs, currentMode);
    var entry = summaryEntry || detailEntry;

    renderMeta(entry);
    renderModeSelector(summaryData, detailData);
    renderSummaryTable(summaryEntry);
    renderDetails(detailEntry);
    renderBaseline(detailEntry);
    renderEnvironment(entry);
    renderNotes(entry);
  }

  function init() {
    // Check if we're on the benchmark page
    if (!document.querySelector('.benchmark-page')) return;

    Promise.all([
      loadJson('/data/benchmark-summary.json'),
      loadJson('/data/benchmark.json')
    ]).then(function(results) {
      summaryData = results[0];
      detailData = results[1];

      // Find best available entry to set initial mode
      findBestEntry(summaryData) || findBestEntry(detailData);

      renderAll();
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
