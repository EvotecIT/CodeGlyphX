param(
    [string]$OutputPath,
    [string]$CssPath,
    [switch]$Force
)

# Default CSS path - use a relative path that won't get mangled
if (-not $CssPath) {
    $CssPath = "/css/api-docs.css"
}

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
if (-not $OutputPath) {
    $OutputPath = Join-Path $repoRoot "CodeGlyphX.Website" "wwwroot"
}

$fragmentsPath = Join-Path $repoRoot "CodeGlyphX.Website" "wwwroot" "api-fragments"
$headerPath = Join-Path $fragmentsPath "header.html"
$footerPath = Join-Path $fragmentsPath "footer.html"

if (-not (Test-Path $headerPath)) { throw "Missing header fragment at $headerPath" }
if (-not (Test-Path $footerPath)) { throw "Missing footer fragment at $footerPath" }

$header = Get-Content $headerPath -Raw
$footer = Get-Content $footerPath -Raw

function New-StaticPage {
    param(
        [string]$Title,
        [string]$Description,
        [string]$Content,
        [string]$OutputFile,
        [string]$BodyClass = "",
        [string]$ExtraCss = ""
    )

    $bodyClassAttr = if ($BodyClass) { " class=`"$BodyClass`"" } else { "" }
    $extraCssLink = if ($ExtraCss) { "`n  <link rel=`"stylesheet`" href=`"$ExtraCss`" />" } else { "" }

    $html = @"
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>$Title</title>
  <meta name="description" content="$Description" />
  <link rel="icon" type="image/png" href="/codeglyphx-qr-icon.png" />
  <link rel="apple-touch-icon" href="/codeglyphx-qr-icon.png" />
  <link rel="preconnect" href="https://img.shields.io" crossorigin />
  <link rel="preload" href="/codeglyphx-qr-icon.png" as="image" type="image/png" />
  <link rel="stylesheet" href="$CssPath" />$extraCssLink
  <script>(function(){var theme=localStorage.getItem('theme')||'dark';document.documentElement.setAttribute('data-theme',theme);})();</script>
</head>
<body$bodyClassAttr>
  <div class="page">
$header
<main>
$Content
</main>
$footer
  </div>
  <script src="/js/site.js"></script>
  <script>
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
  </script>
</body>
</html>
"@

    $outDir = Split-Path $OutputFile -Parent
    if (-not (Test-Path $outDir)) {
        New-Item -ItemType Directory -Path $outDir -Force | Out-Null
    }

    Set-Content -Path $OutputFile -Value $html -Encoding UTF8
    Write-Host "  Generated: $OutputFile" -ForegroundColor Green
}

# ============================================================================
# HOME PAGE
# ============================================================================
Write-Host "Generating Home page..." -ForegroundColor Cyan

$homeContent = @'
<!-- Hero Section -->
<section class="hero">
    <div class="hero-content">
        <div class="hero-badge">
            <span class="hero-badge-dot"></span>
            <span>Open Source &bull; Apache 2.0 License</span>
        </div>

        <h1>Generate QR Codes &amp; Barcodes<br/>Without Dependencies</h1>

        <p class="hero-tagline">
            CodeGlyphX is a blazing-fast, zero-dependency .NET library for encoding and decoding
            QR codes, Data Matrix, PDF417, Aztec, and all major 1D barcode formats.
            No System.Drawing. No SkiaSharp. Just pure .NET.
        </p>

        <div class="hero-buttons">
            <a href="/playground/" class="btn btn-primary">
                <svg class="btn-icon" viewBox="0 0 24 24" fill="currentColor">
                    <path d="M8 5v14l11-7z"/>
                </svg>
                Try the Playground
            </a>
            <a href="https://www.nuget.org/packages/CodeGlyphX" target="_blank" class="btn btn-secondary">
                <svg class="btn-icon" viewBox="0 0 24 24" fill="currentColor">
                    <path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"/>
                </svg>
                View on NuGet
            </a>
        </div>

        <div class="install-command">
            <code>dotnet add package CodeGlyphX</code>
            <button class="copy-btn" type="button" data-copy="dotnet add package CodeGlyphX" title="Copy install command">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="9" y="9" width="13" height="13" rx="2"/>
                    <path d="M5 15H4a2 2 0 01-2-2V4a2 2 0 012-2h9a2 2 0 012 2v1"/>
                </svg>
            </button>
        </div>

        <div class="hero-badges">
            <a href="https://github.com/EvotecIT/CodeGlyphX" target="_blank" rel="noopener" class="hero-badge-link">
                <img src="https://img.shields.io/github/stars/EvotecIT/CodeGlyphX?style=social" alt="GitHub stars" width="100" height="20" loading="lazy" />
            </a>
            <a href="https://www.nuget.org/packages/CodeGlyphX" target="_blank" rel="noopener" class="hero-badge-link">
                <img src="https://img.shields.io/nuget/dt/CodeGlyphX" alt="NuGet downloads" width="100" height="20" loading="lazy" />
            </a>
        </div>

        <div class="hero-code-preview">
            <div class="code-preview-item qr-preview" title="QR Code">
                <svg viewBox="0 0 21 21" fill="currentColor">
                    <!-- Top-left finder: 7x7 with hollow center -->
                    <path d="M0,0h7v7h-7zM1,1v5h5v-5zM2,2h3v3h-3z"/>
                    <!-- Top-right finder -->
                    <path d="M14,0h7v7h-7zM15,1v5h5v-5zM16,2h3v3h-3z"/>
                    <!-- Bottom-left finder -->
                    <path d="M0,14h7v7h-7zM1,15v5h5v-5zM2,16h3v3h-3z"/>
                    <!-- Timing patterns -->
                    <rect x="8" y="6" width="1" height="1"/><rect x="10" y="6" width="1" height="1"/><rect x="12" y="6" width="1" height="1"/>
                    <rect x="6" y="8" width="1" height="1"/><rect x="6" y="10" width="1" height="1"/><rect x="6" y="12" width="1" height="1"/>
                    <!-- Data area -->
                    <rect x="8" y="8" width="1" height="1"/><rect x="10" y="8" width="1" height="1"/><rect x="12" y="8" width="1" height="1"/>
                    <rect x="9" y="9" width="1" height="1"/><rect x="11" y="9" width="1" height="1"/>
                    <rect x="8" y="10" width="1" height="1"/><rect x="10" y="10" width="1" height="1"/><rect x="12" y="10" width="1" height="1"/>
                    <rect x="9" y="11" width="1" height="1"/><rect x="11" y="11" width="1" height="1"/>
                    <rect x="8" y="12" width="1" height="1"/><rect x="10" y="12" width="1" height="1"/><rect x="12" y="12" width="1" height="1"/>
                    <rect x="14" y="8" width="1" height="1"/><rect x="16" y="9" width="1" height="1"/><rect x="18" y="8" width="1" height="1"/><rect x="15" y="10" width="1" height="1"/><rect x="17" y="11" width="1" height="1"/><rect x="19" y="10" width="1" height="1"/>
                    <rect x="8" y="14" width="1" height="1"/><rect x="10" y="15" width="1" height="1"/><rect x="12" y="14" width="1" height="1"/><rect x="9" y="16" width="1" height="1"/><rect x="11" y="17" width="1" height="1"/>
                    <rect x="14" y="14" width="1" height="1"/><rect x="16" y="15" width="1" height="1"/><rect x="18" y="14" width="1" height="1"/><rect x="15" y="16" width="1" height="1"/><rect x="17" y="17" width="1" height="1"/><rect x="19" y="16" width="1" height="1"/><rect x="20" y="18" width="1" height="1"/>
                </svg>
            </div>
            <div class="code-preview-item barcode-preview" title="Code 128">
                <svg viewBox="0 0 67 40" fill="currentColor">
                    <rect x="0" y="0" width="2" height="40"/><rect x="3" y="0" width="1" height="40"/><rect x="5" y="0" width="2" height="40"/><rect x="9" y="0" width="1" height="40"/><rect x="11" y="0" width="3" height="40"/><rect x="15" y="0" width="1" height="40"/><rect x="18" y="0" width="1" height="40"/><rect x="20" y="0" width="2" height="40"/><rect x="24" y="0" width="1" height="1"/><rect x="26" y="0" width="3" height="40"/><rect x="31" y="0" width="1" height="40"/><rect x="33" y="0" width="2" height="40"/><rect x="37" y="0" width="1" height="40"/><rect x="39" y="0" width="1" height="40"/><rect x="42" y="0" width="2" height="40"/><rect x="45" y="0" width="1" height="40"/><rect x="48" y="0" width="3" height="40"/><rect x="52" y="0" width="1" height="40"/><rect x="55" y="0" width="2" height="40"/><rect x="58" y="0" width="1" height="40"/><rect x="61" y="0" width="1" height="40"/><rect x="63" y="0" width="2" height="40"/><rect x="66" y="0" width="1" height="40"/>
                </svg>
            </div>
            <div class="code-preview-item matrix-preview" title="Data Matrix">
                <svg viewBox="0 0 14 14" fill="currentColor">
                    <!-- L-finder: solid left column and top row -->
                    <rect x="0" y="0" width="14" height="1"/><rect x="0" y="1" width="1" height="13"/>
                    <!-- Clock track: alternating on right and bottom -->
                    <rect x="13" y="1" width="1" height="1"/><rect x="13" y="3" width="1" height="1"/><rect x="13" y="5" width="1" height="1"/><rect x="13" y="7" width="1" height="1"/><rect x="13" y="9" width="1" height="1"/><rect x="13" y="11" width="1" height="1"/><rect x="13" y="13" width="1" height="1"/>
                    <rect x="2" y="13" width="1" height="1"/><rect x="4" y="13" width="1" height="1"/><rect x="6" y="13" width="1" height="1"/><rect x="8" y="13" width="1" height="1"/><rect x="10" y="13" width="1" height="1"/><rect x="12" y="13" width="1" height="1"/>
                    <!-- Dense data fill -->
                    <rect x="2" y="2" width="1" height="1"/><rect x="3" y="2" width="1" height="1"/><rect x="5" y="2" width="1" height="1"/><rect x="7" y="2" width="1" height="1"/><rect x="8" y="2" width="1" height="1"/><rect x="10" y="2" width="1" height="1"/><rect x="11" y="2" width="1" height="1"/>
                    <rect x="2" y="3" width="1" height="1"/><rect x="4" y="3" width="1" height="1"/><rect x="6" y="3" width="1" height="1"/><rect x="8" y="3" width="1" height="1"/><rect x="9" y="3" width="1" height="1"/><rect x="11" y="3" width="1" height="1"/>
                    <rect x="3" y="4" width="1" height="1"/><rect x="4" y="4" width="1" height="1"/><rect x="6" y="4" width="1" height="1"/><rect x="7" y="4" width="1" height="1"/><rect x="9" y="4" width="1" height="1"/><rect x="10" y="4" width="1" height="1"/><rect x="12" y="4" width="1" height="1"/>
                    <rect x="2" y="5" width="1" height="1"/><rect x="5" y="5" width="1" height="1"/><rect x="6" y="5" width="1" height="1"/><rect x="8" y="5" width="1" height="1"/><rect x="10" y="5" width="1" height="1"/><rect x="11" y="5" width="1" height="1"/>
                    <rect x="3" y="6" width="1" height="1"/><rect x="4" y="6" width="1" height="1"/><rect x="7" y="6" width="1" height="1"/><rect x="8" y="6" width="1" height="1"/><rect x="9" y="6" width="1" height="1"/><rect x="11" y="6" width="1" height="1"/><rect x="12" y="6" width="1" height="1"/>
                    <rect x="2" y="7" width="1" height="1"/><rect x="4" y="7" width="1" height="1"/><rect x="5" y="7" width="1" height="1"/><rect x="7" y="7" width="1" height="1"/><rect x="9" y="7" width="1" height="1"/><rect x="10" y="7" width="1" height="1"/>
                    <rect x="3" y="8" width="1" height="1"/><rect x="5" y="8" width="1" height="1"/><rect x="6" y="8" width="1" height="1"/><rect x="8" y="8" width="1" height="1"/><rect x="10" y="8" width="1" height="1"/><rect x="11" y="8" width="1" height="1"/><rect x="12" y="8" width="1" height="1"/>
                    <rect x="2" y="9" width="1" height="1"/><rect x="4" y="9" width="1" height="1"/><rect x="6" y="9" width="1" height="1"/><rect x="7" y="9" width="1" height="1"/><rect x="9" y="9" width="1" height="1"/><rect x="11" y="9" width="1" height="1"/>
                    <rect x="3" y="10" width="1" height="1"/><rect x="4" y="10" width="1" height="1"/><rect x="5" y="10" width="1" height="1"/><rect x="7" y="10" width="1" height="1"/><rect x="8" y="10" width="1" height="1"/><rect x="10" y="10" width="1" height="1"/><rect x="12" y="10" width="1" height="1"/>
                    <rect x="2" y="11" width="1" height="1"/><rect x="5" y="11" width="1" height="1"/><rect x="6" y="11" width="1" height="1"/><rect x="8" y="11" width="1" height="1"/><rect x="9" y="11" width="1" height="1"/><rect x="11" y="11" width="1" height="1"/>
                    <rect x="3" y="12" width="1" height="1"/><rect x="4" y="12" width="1" height="1"/><rect x="6" y="12" width="1" height="1"/><rect x="7" y="12" width="1" height="1"/><rect x="9" y="12" width="1" height="1"/><rect x="10" y="12" width="1" height="1"/><rect x="11" y="12" width="1" height="1"/>
                </svg>
            </div>
        </div>
    </div>
</section>

<!-- Stats Section -->
<section class="stats">
    <div class="stats-grid">
        <div class="stat-item">
            <h3>18+</h3>
            <p>Barcode Formats</p>
        </div>
        <div class="stat-item">
            <h3>15+</h3>
            <p>Output Formats</p>
        </div>
        <div class="stat-item">
            <h3>0</h3>
            <p>Dependencies</p>
        </div>
        <div class="stat-item">
            <h3>3</h3>
            <p>Platforms</p>
        </div>
    </div>
</section>

<!-- Features Section -->
<section class="features">
    <div class="section-header">
        <span class="section-label">Why CodeGlyphX?</span>
        <h2>Built for Modern .NET Development</h2>
        <p>Everything you need for barcode generation and scanning, with none of the bloat.</p>
    </div>

    <div class="features-grid">
        <div class="feature-card">
            <div class="feature-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                    <path d="M13 10V3L4 14h7v7l9-11h-7z"/>
                </svg>
            </div>
            <h3>Zero Dependencies</h3>
            <p>No System.Drawing, SkiaSharp, or ImageSharp required. Pure managed code that deploys anywhere without native library headaches.</p>
        </div>

        <div class="feature-card">
            <div class="feature-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                    <path d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
                </svg>
            </div>
            <h3>Encode &amp; Decode</h3>
            <p>Full round-trip support. Generate codes and read them back from PNG, JPEG, BMP, GIF, and more with robust pixel detection.</p>
        </div>

        <div class="feature-card">
            <div class="feature-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                    <rect x="3" y="3" width="7" height="7" rx="1"/>
                    <rect x="14" y="3" width="7" height="7" rx="1"/>
                    <rect x="3" y="14" width="7" height="7" rx="1"/>
                    <rect x="14" y="14" width="7" height="7" rx="1"/>
                </svg>
            </div>
            <h3>2D Codes</h3>
            <p>QR Code, Micro QR, Data Matrix, PDF417, and Aztec with full ECI, Kanji, structured append, and FNC1/GS1 support.</p>
        </div>

        <div class="feature-card">
            <div class="feature-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                    <rect x="2" y="6" width="2" height="12"/>
                    <rect x="5" y="6" width="1" height="12"/>
                    <rect x="7" y="6" width="3" height="12"/>
                    <rect x="11" y="6" width="1" height="12"/>
                    <rect x="13" y="6" width="2" height="12"/>
                    <rect x="16" y="6" width="1" height="12"/>
                    <rect x="18" y="6" width="2" height="12"/>
                    <rect x="21" y="6" width="1" height="12"/>
                </svg>
            </div>
            <h3>1D Barcodes</h3>
            <p>Code 128, GS1-128, Code 39, Code 93, Codabar, MSI, Plessey, EAN-8/13, UPC-A/E, ITF-14, and more.</p>
        </div>

        <div class="feature-card">
            <div class="feature-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                    <path d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"/>
                </svg>
            </div>
            <h3>Multiple Outputs</h3>
            <p>Render to PNG, JPEG, BMP, SVG, SVGZ, PDF, EPS, HTML, ASCII art, and exotic formats like PPM, PBM, TGA, ICO, XBM, XPM.</p>
        </div>

        <div class="feature-card">
            <div class="feature-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                    <path d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"/>
                </svg>
            </div>
            <h3>AOT &amp; Trim Ready</h3>
            <p>No reflection, no runtime codegen. Fully compatible with Native AOT publishing and aggressive trimming.</p>
        </div>

        <div class="feature-card">
            <div class="feature-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                    <path d="M21 12a9 9 0 01-9 9m9-9a9 9 0 00-9-9m9 9H3m9 9a9 9 0 01-9-9m9 9c1.657 0 3-4.03 3-9s-1.343-9-3-9m0 18c-1.657 0-3-4.03-3-9s1.343-9 3-9m-9 9a9 9 0 019-9"/>
                </svg>
            </div>
            <h3>Cross-Platform</h3>
            <p>Runs identically on Windows, Linux, and macOS. Targets .NET 8+, .NET Standard 2.0, and .NET Framework 4.7.2.</p>
        </div>

        <div class="feature-card">
            <div class="feature-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                    <path d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"/>
                </svg>
            </div>
            <h3>Payment Helpers</h3>
            <p>Built-in support for SEPA Girocode, Swiss QR Bill, BezahlCode, UPI, and cryptocurrency addresses.</p>
        </div>

        <div class="feature-card">
            <div class="feature-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                    <path d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"/>
                </svg>
            </div>
            <h3>OTP Support</h3>
            <p>Generate otpauth:// TOTP and HOTP codes compatible with Google Authenticator, Microsoft Authenticator, and Authy.</p>
        </div>
    </div>
</section>

<!-- Symbologies Section -->
<section class="symbologies">
    <div class="section-header">
        <span class="section-label">Supported Formats</span>
        <h2>Every Barcode You Need</h2>
        <p>From retail EAN codes to industrial Data Matrix, CodeGlyphX has you covered.</p>
    </div>

    <div class="symbology-category">
        <h3>2D Matrix Codes</h3>
        <div class="symbology-grid">
            <span class="symbology-tag">QR Code</span>
            <span class="symbology-tag">Micro QR</span>
            <span class="symbology-tag">Data Matrix</span>
            <span class="symbology-tag">PDF417</span>
            <span class="symbology-tag">Aztec</span>
        </div>
    </div>

    <div class="symbology-category">
        <h3>1D Linear Barcodes</h3>
        <div class="symbology-grid">
            <span class="symbology-tag">Code 128</span>
            <span class="symbology-tag">GS1-128</span>
            <span class="symbology-tag">Code 39</span>
            <span class="symbology-tag">Code 93</span>
            <span class="symbology-tag">Code 11</span>
            <span class="symbology-tag">Codabar</span>
            <span class="symbology-tag">MSI</span>
            <span class="symbology-tag">Plessey</span>
            <span class="symbology-tag">EAN-8</span>
            <span class="symbology-tag">EAN-13</span>
            <span class="symbology-tag">UPC-A</span>
            <span class="symbology-tag">UPC-E</span>
            <span class="symbology-tag">ITF-14</span>
        </div>
    </div>

    <div class="symbology-category">
        <h3>Payload Types</h3>
        <div class="symbology-grid">
            <span class="symbology-tag">URL / Text</span>
            <span class="symbology-tag">WiFi Config</span>
            <span class="symbology-tag">vCard Contact</span>
            <span class="symbology-tag">MeCard</span>
            <span class="symbology-tag">Calendar Event</span>
            <span class="symbology-tag">Email / Phone / SMS</span>
            <span class="symbology-tag">TOTP / HOTP</span>
            <span class="symbology-tag">SEPA Girocode</span>
            <span class="symbology-tag">Swiss QR Bill</span>
            <span class="symbology-tag">UPI Payment</span>
            <span class="symbology-tag">Bitcoin / Crypto</span>
            <span class="symbology-tag">App Store Links</span>
        </div>
    </div>
</section>

<!-- Code Examples Section -->
<section class="code-examples">
    <div class="section-header">
        <span class="section-label">Simple API</span>
        <h2>One Line of Code</h2>
        <p>Generate any barcode format with intuitive, discoverable APIs.</p>
    </div>

    <div class="code-example-container">
        <pre class="code-block"><span class="keyword">using</span> CodeGlyphX;

<span class="comment">// QR Code - one liner</span>
QR.Save(<span class="string">"https://evotec.xyz"</span>, <span class="string">"website.png"</span>);
QR.Save(<span class="string">"https://evotec.xyz"</span>, <span class="string">"website.svg"</span>);
QR.Save(<span class="string">"https://evotec.xyz"</span>, <span class="string">"website.pdf"</span>);

<span class="comment">// Barcodes</span>
Barcode.Save(BarcodeType.Code128, <span class="string">"PRODUCT-12345"</span>, <span class="string">"barcode.png"</span>);
Barcode.Save(BarcodeType.Ean13, <span class="string">"5901234123457"</span>, <span class="string">"ean.png"</span>);

<span class="comment">// 2D codes</span>
DataMatrixCode.Save(<span class="string">"Serial: ABC123"</span>, <span class="string">"datamatrix.png"</span>);
Pdf417Code.Save(<span class="string">"Document ID: 98765"</span>, <span class="string">"pdf417.png"</span>);
AztecCode.Save(<span class="string">"Ticket: CONF-2024"</span>, <span class="string">"aztec.png"</span>);

<span class="comment">// Decode from image</span>
<span class="keyword">if</span> (QrImageDecoder.TryDecodeImage(File.ReadAllBytes(<span class="string">"qr.png"</span>), <span class="keyword">out var</span> result))
{
    Console.WriteLine(result.Text);
}</pre>
    </div>
</section>

<!-- Payload Examples -->
<section class="code-examples" style="background: var(--bg-card);">
    <div class="section-header">
        <span class="section-label">Rich Payloads</span>
        <h2>More Than Just Text</h2>
        <p>Built-in helpers for WiFi, contacts, payments, authentication, and more.</p>
    </div>

    <div class="code-example-container">
        <pre class="code-block"><span class="keyword">using</span> CodeGlyphX;
<span class="keyword">using</span> CodeGlyphX.Payloads;

<span class="comment">// WiFi configuration</span>
QR.Save(QrPayloads.Wifi(<span class="string">"MyNetwork"</span>, <span class="string">"SecurePassword123"</span>), <span class="string">"wifi.png"</span>);

<span class="comment">// 2FA / OTP codes (Google Authenticator, Authy, etc.)</span>
QR.Save(QrPayloads.OneTimePassword(
    OtpAuthType.Totp,
    <span class="string">"JBSWY3DPEHPK3PXP"</span>,
    label: <span class="string">"user@example.com"</span>,
    issuer: <span class="string">"MyApp"</span>
), <span class="string">"otp.png"</span>);

<span class="comment">// Contact card</span>
QR.Save(QrPayloads.VCard(
    firstName: <span class="string">"Przemyslaw"</span>,
    lastName: <span class="string">"Klys"</span>,
    email: <span class="string">"contact@evotec.pl"</span>,
    website: <span class="string">"https://evotec.xyz"</span>
), <span class="string">"contact.png"</span>);

<span class="comment">// SEPA payment (European bank transfer)</span>
QR.Save(QrPayloads.Girocode(
    iban: <span class="string">"DE89370400440532013000"</span>,
    bic: <span class="string">"COBADEFFXXX"</span>,
    recipientName: <span class="string">"Evotec Services"</span>,
    amount: 99.99m,
    reference: <span class="string">"Invoice 2024-001"</span>
), <span class="string">"payment.png"</span>);</pre>
    </div>
</section>

<!-- CTA Section -->
<section class="cta">
    <div class="cta-content">
        <h2>Ready to Get Started?</h2>
        <p>
            Install CodeGlyphX via NuGet and start generating barcodes in minutes.
            No configuration, no native dependencies, just pure .NET.
        </p>
        <div class="hero-buttons" style="margin-bottom: 0;">
            <a href="https://www.nuget.org/packages/CodeGlyphX" target="_blank" class="btn btn-primary">
                Install from NuGet
            </a>
            <a href="https://github.com/EvotecIT/CodeGlyphX" target="_blank" class="btn btn-secondary">
                View Source on GitHub
            </a>
        </div>
    </div>
</section>

<!-- About Section -->
<section style="padding: 4rem 2rem; text-align: center;">
    <div style="max-width: 700px; margin: 0 auto;">
        <p style="color: var(--text-muted); font-size: 1rem; line-height: 1.8;">
            <strong style="color: var(--text);">CodeGlyphX</strong> is developed and maintained by
            <a href="https://twitter.com/PrzemyslawKlys" target="_blank">Przemyslaw Klys</a>
            at <a href="https://evotec.xyz" target="_blank">Evotec Services sp. z o.o.</a>
            <br/><br/>
            We build open-source tools for the .NET ecosystem, including PowerShell modules,
            libraries, and developer utilities. Check out our other projects on
            <a href="https://github.com/EvotecIT" target="_blank">GitHub</a>.
        </p>
    </div>
</section>
'@

New-StaticPage `
    -Title "CodeGlyphX - Zero-Dependency QR & Barcode Toolkit for .NET" `
    -Description "CodeGlyphX is a blazing-fast, zero-dependency .NET library for generating and decoding QR codes, barcodes, Data Matrix, PDF417, and Aztec codes." `
    -Content $homeContent `
    -OutputFile (Join-Path $OutputPath "index.html")

# ============================================================================
# DOCS INDEX PAGE
# ============================================================================
Write-Host "Generating Docs index page..." -ForegroundColor Cyan

$docsIndexContent = @'
<div class="docs-layout">
    <button class="docs-sidebar-toggle" aria-label="Toggle documentation menu">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="20" height="20">
            <path d="M4 6h16M4 12h16M4 18h16"/>
        </svg>
        <span>Documentation Menu</span>
    </button>
    <div class="docs-sidebar-overlay"></div>
    <aside class="docs-sidebar">
        <nav class="docs-nav">
            <div class="docs-nav-section">
                <div class="docs-nav-title">Getting Started</div>
                <a href="#introduction" class="active">Introduction</a>
                <a href="#installation">Installation</a>
                <a href="#quickstart">Quick Start</a>
            </div>

            <div class="docs-nav-section">
                <div class="docs-nav-title">2D Codes</div>
                <a href="#qr">QR Code</a>
                <a href="#microqr">Micro QR</a>
                <a href="#datamatrix">Data Matrix</a>
                <a href="#pdf417">PDF417</a>
                <a href="#aztec">Aztec</a>
            </div>

            <div class="docs-nav-section">
                <div class="docs-nav-title">1D Barcodes</div>
                <a href="#code128">Code 128 / GS1-128</a>
                <a href="#code39">Code 39 / 93</a>
                <a href="#ean-upc">EAN / UPC</a>
            </div>

            <div class="docs-nav-section">
                <div class="docs-nav-title">Features</div>
                <a href="#payloads">Payload Helpers</a>
                <a href="#decoding">Image Decoding</a>
                <a href="#renderers">Output Formats</a>
                <a href="#benchmarks">Benchmarks</a>
            </div>

            <div class="docs-nav-section">
                <div class="docs-nav-title">Reference</div>
                <a href="/api/">API Reference →</a>
            </div>
        </nav>
    </aside>

    <div class="docs-content">
        <div class="edit-on-github">
            <a href="https://github.com/EvotecIT/CodeGlyphX/edit/master/CodeGlyphX.Website/Pages/Docs.razor" target="_blank" rel="noopener">
                <svg viewBox="0 0 24 24" fill="currentColor" width="16" height="16">
                    <path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/>
                </svg>
                Edit on GitHub
            </a>
        </div>

        <section id="introduction">
            <h1>CodeGlyphX Documentation</h1>
            <p>
                Welcome to the CodeGlyphX documentation. CodeGlyphX is a zero-dependency .NET library
                for generating and decoding QR codes, barcodes, and other 2D matrix codes.
            </p>

            <h2>Key Features</h2>
            <ul style="color: var(--text-muted); margin-left: 1.5rem;">
                <li><strong>Zero external dependencies</strong> - No System.Drawing, SkiaSharp, or ImageSharp required</li>
                <li><strong>Full encode &amp; decode</strong> - Round-trip support for all symbologies</li>
                <li><strong>Multiple output formats</strong> - PNG, SVG, PDF, EPS, HTML, and many more</li>
                <li><strong>Cross-platform</strong> - Windows, Linux, macOS</li>
                <li><strong>AOT compatible</strong> - Works with Native AOT and trimming</li>
            </ul>

            <h2>Supported Symbologies</h2>
            <h3>2D Matrix Codes</h3>
            <p>QR Code, Micro QR, Data Matrix, PDF417, Aztec</p>

            <h3>1D Linear Barcodes</h3>
            <p>Code 128, GS1-128, Code 39, Code 93, Code 11, Codabar, MSI, Plessey, EAN-8, EAN-13, UPC-A, UPC-E, ITF-14</p>

            <h2>Quick Example</h2>
            <pre class="code-block">using CodeGlyphX;

// Generate a QR code
QR.Save("https://evotec.xyz", "website.png");

// Generate a barcode
Barcode.Save(BarcodeType.Code128, "PRODUCT-123", "barcode.png");

// Decode an image
if (QrImageDecoder.TryDecodeImage(imageBytes, out var result))
{
    Console.WriteLine(result.Text);
}</pre>

            <h2>Getting Help</h2>
            <p>
                If you encounter issues or have questions, please visit the
                <a href="https://github.com/EvotecIT/CodeGlyphX/issues" target="_blank">GitHub Issues</a> page.
            </p>
        </section>

        <section id="installation">
            <h1>Installation</h1>
            <p>CodeGlyphX is available as a NuGet package and can be installed in several ways.</p>

            <h2>.NET CLI</h2>
            <pre class="code-block">dotnet add package CodeGlyphX</pre>

            <h2>Package Manager Console</h2>
            <pre class="code-block">Install-Package CodeGlyphX</pre>

            <h2>PackageReference</h2>
            <p>Add the following to your <code>.csproj</code> file:</p>
            <pre class="code-block">&lt;PackageReference Include="CodeGlyphX" Version="*" /&gt;</pre>

            <h2>Supported Frameworks</h2>
            <ul style="color: var(--text-muted); margin-left: 1.5rem;">
                <li><strong>.NET 8.0+</strong> - Full support, no additional dependencies</li>
                <li><strong>.NET Standard 2.0</strong> - Requires System.Memory 4.5.5</li>
                <li><strong>.NET Framework 4.7.2+</strong> - Requires System.Memory 4.5.5</li>
            </ul>
        </section>

        <section id="quickstart">
            <h1>Quick Start</h1>
            <p>Get up and running with CodeGlyphX in under a minute.</p>

            <h2>1. Install the Package</h2>
            <pre class="code-block">dotnet add package CodeGlyphX</pre>

            <h2>2. Generate Your First QR Code</h2>
            <pre class="code-block">using CodeGlyphX;

// Create a QR code and save to file
QR.Save("Hello, World!", "hello.png");

// The output format is determined by the file extension
QR.Save("Hello, World!", "hello.svg");  // Vector SVG
QR.Save("Hello, World!", "hello.pdf");  // PDF document</pre>

            <h2>3. Generate Barcodes</h2>
            <pre class="code-block">using CodeGlyphX;

// Code 128 barcode
Barcode.Save(BarcodeType.Code128, "PRODUCT-12345", "barcode.png");

// EAN-13 (retail products)
Barcode.Save(BarcodeType.Ean13, "5901234123457", "ean.png");</pre>

            <h2>4. Decode Images</h2>
            <pre class="code-block">using CodeGlyphX;

var imageBytes = File.ReadAllBytes("qrcode.png");

if (QrImageDecoder.TryDecodeImage(imageBytes, out var result))
{
    Console.WriteLine($"Decoded: {result.Text}");
}</pre>
        </section>

        <section id="benchmarks">
            <h1>Benchmarks</h1>
            <p>
                Benchmarks are run locally using BenchmarkDotNet. Results below were captured on
                2026-01-18 (Ubuntu 24.04, Ryzen 9 9950X, .NET 8.0.22). Your results will vary.
            </p>

            <h2>QR (Encode)</h2>
            <table style="width: 100%; border-collapse: collapse; margin: 1rem 0;">
                <thead>
                    <tr style="border-bottom: 1px solid var(--border);">
                        <th style="text-align: left; padding: 0.75rem;">Scenario</th>
                        <th style="text-align: left; padding: 0.75rem;">Mean (us)</th>
                        <th style="text-align: left; padding: 0.75rem;">Allocated</th>
                    </tr>
                </thead>
                <tbody style="color: var(--text-muted);">
                    <tr style="border-bottom: 1px solid var(--border);">
                        <td style="padding: 0.75rem;">QR PNG (short)</td>
                        <td style="padding: 0.75rem;">331.33</td>
                        <td style="padding: 0.75rem;">431.94 KB</td>
                    </tr>
                    <tr style="border-bottom: 1px solid var(--border);">
                        <td style="padding: 0.75rem;">QR PNG (medium)</td>
                        <td style="padding: 0.75rem;">713.68</td>
                        <td style="padding: 0.75rem;">837.75 KB</td>
                    </tr>
                    <tr>
                        <td style="padding: 0.75rem;">QR PNG (long)</td>
                        <td style="padding: 0.75rem;">2197.99</td>
                        <td style="padding: 0.75rem;">3041.06 KB</td>
                    </tr>
                </tbody>
            </table>

            <h2>Run the Benchmarks</h2>
            <pre class="code-block">dotnet run -c Release --framework net8.0 --project CodeGlyphX.Benchmarks/CodeGlyphX.Benchmarks.csproj -- --filter "*"</pre>
        </section>

        <section id="qr">
            <h1>QR Code Generation</h1>
            <p>CodeGlyphX provides comprehensive QR code support including standard QR and Micro QR formats.</p>

            <h2>Basic Usage</h2>
            <pre class="code-block">using CodeGlyphX;

// Simple one-liner
QR.Save("https://example.com", "qr.png");

// With error correction level
QR.Save("https://example.com", "qr.png", QrErrorCorrection.H);</pre>

            <h2>Error Correction Levels</h2>
            <table style="width: 100%; border-collapse: collapse; margin: 1rem 0;">
                <thead>
                    <tr style="border-bottom: 1px solid var(--border);">
                        <th style="text-align: left; padding: 0.75rem;">Level</th>
                        <th style="text-align: left; padding: 0.75rem;">Recovery</th>
                        <th style="text-align: left; padding: 0.75rem;">Use Case</th>
                    </tr>
                </thead>
                <tbody style="color: var(--text-muted);">
                    <tr style="border-bottom: 1px solid var(--border);">
                        <td style="padding: 0.75rem;"><code>L</code></td>
                        <td style="padding: 0.75rem;">~7%</td>
                        <td style="padding: 0.75rem;">Maximum data capacity</td>
                    </tr>
                    <tr style="border-bottom: 1px solid var(--border);">
                        <td style="padding: 0.75rem;"><code>M</code></td>
                        <td style="padding: 0.75rem;">~15%</td>
                        <td style="padding: 0.75rem;">Default, balanced</td>
                    </tr>
                    <tr style="border-bottom: 1px solid var(--border);">
                        <td style="padding: 0.75rem;"><code>Q</code></td>
                        <td style="padding: 0.75rem;">~25%</td>
                        <td style="padding: 0.75rem;">Higher reliability</td>
                    </tr>
                    <tr>
                        <td style="padding: 0.75rem;"><code>H</code></td>
                        <td style="padding: 0.75rem;">~30%</td>
                        <td style="padding: 0.75rem;">Maximum error correction</td>
                    </tr>
                </tbody>
            </table>

            <h2>Styling Options</h2>
            <pre class="code-block">using CodeGlyphX;

var options = new QrEasyOptions
{
    ModuleShape = QrPngModuleShape.Rounded,
    ModuleCornerRadiusPx = 3,
    Eyes = new QrPngEyeOptions
    {
        UseFrame = true,
        OuterShape = QrPngModuleShape.Circle,
        InnerShape = QrPngModuleShape.Circle,
        OuterColor = new Rgba32(220, 20, 60),
        InnerColor = new Rgba32(220, 20, 60)
    }
};

QR.Save("https://example.com", "styled-qr.png", options);</pre>
        </section>

        <section id="payloads">
            <h1>Payload Helpers</h1>
            <p>CodeGlyphX includes built-in helpers for generating QR codes with structured payloads that mobile devices can interpret.</p>

            <h2>WiFi Configuration</h2>
            <pre class="code-block">using CodeGlyphX;
using CodeGlyphX.Payloads;

// WPA/WPA2 network
QR.Save(QrPayloads.Wifi("NetworkName", "Password123"), "wifi.png");

// Open network (no password)
QR.Save(QrPayloads.Wifi("PublicNetwork", null, WifiAuthType.None), "wifi-open.png");</pre>

            <h2>Contact Cards</h2>
            <pre class="code-block">// vCard format (widely supported)
QR.Save(QrPayloads.VCard(
    firstName: "Przemyslaw",
    lastName: "Klys",
    email: "contact@evotec.pl",
    phone: "+48123456789",
    website: "https://evotec.xyz",
    organization: "Evotec Services"
), "contact.png");</pre>

            <h2>OTP / 2FA</h2>
            <pre class="code-block">// TOTP (Time-based One-Time Password)
QR.Save(QrPayloads.OneTimePassword(
    OtpAuthType.Totp,
    secret: "JBSWY3DPEHPK3PXP",
    label: "user@example.com",
    issuer: "MyApp"
), "totp.png");</pre>

            <h2>SEPA Girocode</h2>
            <pre class="code-block">QR.Save(QrPayloads.Girocode(
    iban: "DE89370400440532013000",
    bic: "COBADEFFXXX",
    recipientName: "Evotec Services",
    amount: 99.99m,
    reference: "Invoice-2024-001"
), "sepa.png");</pre>
        </section>

        <section id="renderers">
            <h1>Output Formats</h1>
            <p>CodeGlyphX supports a wide variety of output formats. The format is automatically determined by the file extension.</p>

            <h2>Vector Formats</h2>
            <ul style="color: var(--text-muted); margin-left: 1.5rem;">
                <li><strong>SVG</strong> (<code>.svg</code>) - Scalable, web-friendly</li>
                <li><strong>SVGZ</strong> (<code>.svgz</code>) - Compressed SVG</li>
                <li><strong>PDF</strong> (<code>.pdf</code>) - Vector by default</li>
                <li><strong>EPS</strong> (<code>.eps</code>) - PostScript</li>
            </ul>

            <h2>Raster Formats</h2>
            <ul style="color: var(--text-muted); margin-left: 1.5rem;">
                <li><strong>PNG</strong> (<code>.png</code>) - Lossless, transparent</li>
                <li><strong>JPEG</strong> (<code>.jpg</code>) - Lossy compression</li>
                <li><strong>BMP</strong> (<code>.bmp</code>) - Uncompressed bitmap</li>
                <li><strong>TGA</strong> (<code>.tga</code>) - Targa format</li>
                <li><strong>ICO</strong> (<code>.ico</code>) - Windows icon</li>
            </ul>

            <h2>Special Formats</h2>
            <ul style="color: var(--text-muted); margin-left: 1.5rem;">
                <li><strong>HTML</strong> (<code>.html</code>) - Table-based output</li>
                <li><strong>PPM/PBM/PGM/PAM</strong> - Portable pixel formats</li>
                <li><strong>XBM/XPM</strong> - X Window formats</li>
                <li><strong>ASCII</strong> - Text representation (API only)</li>
            </ul>

            <h2>Programmatic Rendering</h2>
            <pre class="code-block">using CodeGlyphX;

// Get raw PNG bytes
byte[] pngBytes = QrEasy.RenderPng("Hello", QrErrorCorrection.M, moduleSize: 10);

// Get SVG string
string svg = QrEasy.RenderSvg("Hello", QrErrorCorrection.M);

// Get Base64 data URI
string dataUri = QrEasy.RenderPngBase64DataUri("Hello");</pre>
        </section>

        <section id="datamatrix">
            <h1>Data Matrix</h1>
            <p>Data Matrix is a 2D barcode widely used in industrial and commercial applications for marking small items.</p>

            <h2>Basic Usage</h2>
            <pre class="code-block">using CodeGlyphX;

// Simple Data Matrix
DataMatrixCode.Save("SERIAL-12345", "datamatrix.png");

// With specific size
DataMatrixCode.Save("SERIAL-12345", "datamatrix.svg", size: DataMatrixSize.Square24);</pre>

            <h2>Use Cases</h2>
            <ul style="color: var(--text-muted); margin-left: 1.5rem;">
                <li><strong>Electronics manufacturing</strong> - Component marking and tracking</li>
                <li><strong>Healthcare</strong> - Medical device identification (UDI)</li>
                <li><strong>Aerospace</strong> - Part serialization</li>
                <li><strong>Postal services</strong> - High-density mail sorting</li>
            </ul>

            <h2>Features</h2>
            <p>CodeGlyphX supports all standard Data Matrix sizes from 10x10 to 144x144 modules, including rectangular variants.</p>
        </section>

        <section id="pdf417">
            <h1>PDF417</h1>
            <p>PDF417 is a stacked linear barcode capable of encoding large amounts of data, commonly used in ID cards and transport tickets.</p>

            <h2>Basic Usage</h2>
            <pre class="code-block">using CodeGlyphX;

// Simple PDF417
Pdf417Code.Save("Document content here", "pdf417.png");

// With error correction level
Pdf417Code.Save("Document content", "pdf417.png", errorCorrectionLevel: 5);</pre>

            <h2>Use Cases</h2>
            <ul style="color: var(--text-muted); margin-left: 1.5rem;">
                <li><strong>Government IDs</strong> - Driver's licenses, ID cards</li>
                <li><strong>Travel documents</strong> - Boarding passes, tickets</li>
                <li><strong>Shipping</strong> - Package labels with detailed info</li>
                <li><strong>Inventory</strong> - Large data capacity for detailed records</li>
            </ul>

            <h2>Data Capacity</h2>
            <p>PDF417 can encode up to 1,850 alphanumeric characters or 2,710 numeric digits.</p>
        </section>

        <section id="aztec">
            <h1>Aztec Code</h1>
            <p>Aztec is a 2D matrix barcode designed for high readability even when printed at low resolution or on curved surfaces.</p>

            <h2>Basic Usage</h2>
            <pre class="code-block">using CodeGlyphX;

// Simple Aztec code
AztecCode.Save("Ticket: CONF-2024-001", "aztec.png");

// With error correction percentage
AztecCode.Save("Ticket data", "aztec.png", errorCorrectionPercent: 33);</pre>

            <h2>Use Cases</h2>
            <ul style="color: var(--text-muted); margin-left: 1.5rem;">
                <li><strong>Transportation</strong> - Train and airline tickets</li>
                <li><strong>Event tickets</strong> - Mobile ticketing apps</li>
                <li><strong>Patient wristbands</strong> - Healthcare identification</li>
                <li><strong>Curved surfaces</strong> - Bottles, tubes, cylinders</li>
            </ul>

            <h2>Advantages</h2>
            <p>Aztec codes don't require a quiet zone around them and can be read even when partially damaged, making them ideal for mobile ticketing.</p>
        </section>

        <section id="code128">
            <h1>Code 128 / GS1-128</h1>
            <p>Code 128 is a high-density linear barcode supporting the full ASCII character set. GS1-128 is an application standard that uses Code 128.</p>

            <h2>Basic Usage</h2>
            <pre class="code-block">using CodeGlyphX;

// Code 128
Barcode.Save(BarcodeType.Code128, "PRODUCT-12345", "code128.png");

// GS1-128 with Application Identifiers
Barcode.Save(BarcodeType.Gs1128, "(01)09501101530003(17)250101", "gs1.png");</pre>

            <h2>Character Sets</h2>
            <table style="width: 100%; border-collapse: collapse; margin: 1rem 0;">
                <thead>
                    <tr style="border-bottom: 1px solid var(--border);">
                        <th style="text-align: left; padding: 0.75rem;">Set</th>
                        <th style="text-align: left; padding: 0.75rem;">Characters</th>
                        <th style="text-align: left; padding: 0.75rem;">Best For</th>
                    </tr>
                </thead>
                <tbody style="color: var(--text-muted);">
                    <tr style="border-bottom: 1px solid var(--border);">
                        <td style="padding: 0.75rem;"><code>A</code></td>
                        <td style="padding: 0.75rem;">A-Z, 0-9, control chars</td>
                        <td style="padding: 0.75rem;">Alphanumeric with controls</td>
                    </tr>
                    <tr style="border-bottom: 1px solid var(--border);">
                        <td style="padding: 0.75rem;"><code>B</code></td>
                        <td style="padding: 0.75rem;">A-Z, a-z, 0-9, symbols</td>
                        <td style="padding: 0.75rem;">General text (most common)</td>
                    </tr>
                    <tr>
                        <td style="padding: 0.75rem;"><code>C</code></td>
                        <td style="padding: 0.75rem;">00-99 (digit pairs)</td>
                        <td style="padding: 0.75rem;">Numeric data (most compact)</td>
                    </tr>
                </tbody>
            </table>
        </section>

        <section id="code39">
            <h1>Code 39 / Code 93</h1>
            <p>Code 39 and Code 93 are widely used linear barcodes, particularly in automotive and defense industries.</p>

            <h2>Basic Usage</h2>
            <pre class="code-block">using CodeGlyphX;

// Code 39
Barcode.Save(BarcodeType.Code39, "HELLO-123", "code39.png");

// Code 93 (more compact)
Barcode.Save(BarcodeType.Code93, "HELLO-123", "code93.png");</pre>

            <h2>Valid Characters</h2>
            <p>Code 39 supports: <code>A-Z</code>, <code>0-9</code>, <code>-</code>, <code>.</code>, <code>$</code>, <code>/</code>, <code>+</code>, <code>%</code>, <code>SPACE</code></p>
            <p style="margin-top: 0.5rem;"><strong>Note:</strong> Lowercase letters are automatically converted to uppercase.</p>

            <h2>Comparison</h2>
            <table style="width: 100%; border-collapse: collapse; margin: 1rem 0;">
                <thead>
                    <tr style="border-bottom: 1px solid var(--border);">
                        <th style="text-align: left; padding: 0.75rem;">Feature</th>
                        <th style="text-align: left; padding: 0.75rem;">Code 39</th>
                        <th style="text-align: left; padding: 0.75rem;">Code 93</th>
                    </tr>
                </thead>
                <tbody style="color: var(--text-muted);">
                    <tr style="border-bottom: 1px solid var(--border);">
                        <td style="padding: 0.75rem;">Density</td>
                        <td style="padding: 0.75rem;">Lower</td>
                        <td style="padding: 0.75rem;">~40% more compact</td>
                    </tr>
                    <tr style="border-bottom: 1px solid var(--border);">
                        <td style="padding: 0.75rem;">Checksum</td>
                        <td style="padding: 0.75rem;">Optional</td>
                        <td style="padding: 0.75rem;">Mandatory (2 chars)</td>
                    </tr>
                    <tr>
                        <td style="padding: 0.75rem;">Industry</td>
                        <td style="padding: 0.75rem;">Automotive, Defense</td>
                        <td style="padding: 0.75rem;">Logistics, Postal</td>
                    </tr>
                </tbody>
            </table>
        </section>

        <section id="ean-upc">
            <h1>EAN / UPC Barcodes</h1>
            <p>EAN (European Article Number) and UPC (Universal Product Code) are the standard retail barcodes found on consumer products worldwide.</p>

            <h2>Basic Usage</h2>
            <pre class="code-block">using CodeGlyphX;

// EAN-13 (International)
Barcode.Save(BarcodeType.Ean13, "5901234123457", "ean13.png");

// EAN-8 (Smaller packages)
Barcode.Save(BarcodeType.Ean8, "96385074", "ean8.png");

// UPC-A (North America)
Barcode.Save(BarcodeType.UpcA, "012345678905", "upca.png");

// UPC-E (Compact)
Barcode.Save(BarcodeType.UpcE, "01234565", "upce.png");</pre>

            <h2>Format Guide</h2>
            <table style="width: 100%; border-collapse: collapse; margin: 1rem 0;">
                <thead>
                    <tr style="border-bottom: 1px solid var(--border);">
                        <th style="text-align: left; padding: 0.75rem;">Type</th>
                        <th style="text-align: left; padding: 0.75rem;">Digits</th>
                        <th style="text-align: left; padding: 0.75rem;">Region</th>
                        <th style="text-align: left; padding: 0.75rem;">Use Case</th>
                    </tr>
                </thead>
                <tbody style="color: var(--text-muted);">
                    <tr style="border-bottom: 1px solid var(--border);">
                        <td style="padding: 0.75rem;">EAN-13</td>
                        <td style="padding: 0.75rem;">13</td>
                        <td style="padding: 0.75rem;">International</td>
                        <td style="padding: 0.75rem;">Standard retail products</td>
                    </tr>
                    <tr style="border-bottom: 1px solid var(--border);">
                        <td style="padding: 0.75rem;">EAN-8</td>
                        <td style="padding: 0.75rem;">8</td>
                        <td style="padding: 0.75rem;">International</td>
                        <td style="padding: 0.75rem;">Small packages</td>
                    </tr>
                    <tr style="border-bottom: 1px solid var(--border);">
                        <td style="padding: 0.75rem;">UPC-A</td>
                        <td style="padding: 0.75rem;">12</td>
                        <td style="padding: 0.75rem;">North America</td>
                        <td style="padding: 0.75rem;">Retail products</td>
                    </tr>
                    <tr>
                        <td style="padding: 0.75rem;">UPC-E</td>
                        <td style="padding: 0.75rem;">8</td>
                        <td style="padding: 0.75rem;">North America</td>
                        <td style="padding: 0.75rem;">Small items</td>
                    </tr>
                </tbody>
            </table>
        </section>

        <section id="decoding">
            <h1>Image Decoding</h1>
            <p>CodeGlyphX includes a built-in decoder for reading QR codes and barcodes from images.</p>

            <h2>Basic Usage</h2>
            <pre class="code-block">using CodeGlyphX;

// Decode from file
byte[] imageBytes = File.ReadAllBytes("qrcode.png");

if (QrImageDecoder.TryDecodeImage(imageBytes, out var result))
{
    Console.WriteLine($"Decoded: {result.Text}");
    Console.WriteLine($"Format: {result.BarcodeFormat}");
}

// Decode from stream
using var stream = File.OpenRead("barcode.png");
var decodeResult = QrImageDecoder.DecodeImage(stream);</pre>

            <h2>Supported Formats for Decoding</h2>
            <ul style="color: var(--text-muted); margin-left: 1.5rem;">
                <li><strong>Images:</strong> PNG, JPEG, BMP, GIF</li>
                <li><strong>QR Codes:</strong> Standard QR, Micro QR</li>
                <li><strong>1D Barcodes:</strong> Code 128, Code 39, EAN, UPC</li>
            </ul>

            <h2>Handling Multiple Results</h2>
            <pre class="code-block">// Decode all barcodes in an image
var results = QrImageDecoder.DecodeAllImages(imageBytes);

foreach (var barcode in results)
{
    Console.WriteLine($"{barcode.BarcodeFormat}: {barcode.Text}");
}</pre>
        </section>

        <section id="microqr">
            <h1>Micro QR Code</h1>
            <p>Micro QR is a smaller version of the standard QR code, designed for applications where space is limited.</p>

            <h2>Basic Usage</h2>
            <pre class="code-block">using CodeGlyphX;

// Generate Micro QR
QR.Save("ABC123", "microqr.png", microQr: true);</pre>

            <h2>Comparison with Standard QR</h2>
            <table style="width: 100%; border-collapse: collapse; margin: 1rem 0;">
                <thead>
                    <tr style="border-bottom: 1px solid var(--border);">
                        <th style="text-align: left; padding: 0.75rem;">Feature</th>
                        <th style="text-align: left; padding: 0.75rem;">Standard QR</th>
                        <th style="text-align: left; padding: 0.75rem;">Micro QR</th>
                    </tr>
                </thead>
                <tbody style="color: var(--text-muted);">
                    <tr style="border-bottom: 1px solid var(--border);">
                        <td style="padding: 0.75rem;">Finder patterns</td>
                        <td style="padding: 0.75rem;">3 corners</td>
                        <td style="padding: 0.75rem;">1 corner</td>
                    </tr>
                    <tr style="border-bottom: 1px solid var(--border);">
                        <td style="padding: 0.75rem;">Max capacity</td>
                        <td style="padding: 0.75rem;">~3KB</td>
                        <td style="padding: 0.75rem;">~35 characters</td>
                    </tr>
                    <tr>
                        <td style="padding: 0.75rem;">Best for</td>
                        <td style="padding: 0.75rem;">General use</td>
                        <td style="padding: 0.75rem;">Small labels, PCBs</td>
                    </tr>
                </tbody>
            </table>
        </section>
    </div>
</div>
'@

# Create docs directory
$docsDir = Join-Path $OutputPath "docs"
if (-not (Test-Path $docsDir)) {
    New-Item -ItemType Directory -Path $docsDir -Force | Out-Null
}

New-StaticPage `
    -Title "Documentation - CodeGlyphX" `
    -Description "CodeGlyphX documentation - learn how to generate and decode QR codes, barcodes, and 2D matrix codes in .NET." `
    -Content $docsIndexContent `
    -OutputFile (Join-Path $docsDir "index.html")

# ============================================================================
# FAQ PAGE (Generated from JSON)
# ============================================================================
Write-Host "Generating FAQ page from JSON..." -ForegroundColor Cyan

$faqJsonPath = Join-Path $repoRoot "Assets" "Data" "faq.json"
if (-not (Test-Path $faqJsonPath)) {
    Write-Warning "FAQ JSON not found at $faqJsonPath - skipping FAQ generation"
} else {
    $faqData = Get-Content $faqJsonPath -Raw | ConvertFrom-Json

    # Build FAQ HTML content
    $faqHtmlBuilder = [System.Text.StringBuilder]::new()
    [void]$faqHtmlBuilder.AppendLine('<div class="faq-page">')
    [void]$faqHtmlBuilder.AppendLine('    <div class="faq-hero">')
    [void]$faqHtmlBuilder.AppendLine('        <span class="section-label">Support</span>')
    [void]$faqHtmlBuilder.AppendLine("        <h1>$($faqData.title)</h1>")
    [void]$faqHtmlBuilder.AppendLine("        <p>$($faqData.description)</p>")
    [void]$faqHtmlBuilder.AppendLine('    </div>')
    [void]$faqHtmlBuilder.AppendLine('')
    [void]$faqHtmlBuilder.AppendLine('    <div class="faq-content">')

    foreach ($section in $faqData.sections) {
        [void]$faqHtmlBuilder.AppendLine('        <div class="faq-section">')
        [void]$faqHtmlBuilder.AppendLine("            <h2>$($section.title)</h2>")

        foreach ($item in $section.items) {
            [void]$faqHtmlBuilder.AppendLine("            <div class=`"faq-item`" id=`"$($item.id)`">")
            [void]$faqHtmlBuilder.AppendLine("                <h3>$($item.question)</h3>")
            [void]$faqHtmlBuilder.AppendLine("                $($item.answer)")
            [void]$faqHtmlBuilder.AppendLine('            </div>')
        }

        [void]$faqHtmlBuilder.AppendLine('        </div>')
    }

    [void]$faqHtmlBuilder.AppendLine('    </div>')
    [void]$faqHtmlBuilder.AppendLine('</div>')

    # Build JSON-LD structured data for SEO
    $jsonLdQuestions = @()
    foreach ($section in $faqData.sections) {
        foreach ($item in $section.items) {
            # Strip HTML from answer for JSON-LD
            $plainAnswer = $item.answer -replace '<[^>]+>', ' ' -replace '\s+', ' '
            $plainAnswer = $plainAnswer.Trim()
            # Escape for JSON
            $escapedQuestion = $item.question -replace '\\', '\\' -replace '"', '\"'
            $escapedAnswer = $plainAnswer -replace '\\', '\\' -replace '"', '\"'

            $jsonLdQuestions += @"
        {
            "@type": "Question",
            "name": "$escapedQuestion",
            "acceptedAnswer": {
                "@type": "Answer",
                "text": "$escapedAnswer"
            }
        }
"@
        }
    }

    $jsonLdScript = @"
    <script type="application/ld+json">
    {
        "@context": "https://schema.org",
        "@type": "FAQPage",
        "mainEntity": [
$($jsonLdQuestions -join ",`n")
        ]
    }
    </script>
"@

    $faqContent = $faqHtmlBuilder.ToString()

    # Create FAQ directory
    $faqDir = Join-Path $OutputPath "faq"
    if (-not (Test-Path $faqDir)) {
        New-Item -ItemType Directory -Path $faqDir -Force | Out-Null
    }

    # BreadcrumbList JSON-LD
    $breadcrumbJsonLd = @"
    <script type="application/ld+json">
    {
        "@context": "https://schema.org",
        "@type": "BreadcrumbList",
        "itemListElement": [
            {
                "@type": "ListItem",
                "position": 1,
                "name": "Home",
                "item": "https://codeglyphx.com/"
            },
            {
                "@type": "ListItem",
                "position": 2,
                "name": "FAQ",
                "item": "https://codeglyphx.com/faq/"
            }
        ]
    }
    </script>
"@

    # Custom FAQ page with JSON-LD (override New-StaticPage for this one)
    $faqBodyClassAttr = ""
    $faqHtml = @"
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>FAQ - CodeGlyphX</title>
  <meta name="description" content="$($faqData.description)" />
  <link rel="canonical" href="https://codeglyphx.com/faq/" />
  <link rel="icon" type="image/png" href="/codeglyphx-qr-icon.png" />
  <link rel="apple-touch-icon" href="/codeglyphx-qr-icon.png" />
  <link rel="preconnect" href="https://img.shields.io" crossorigin />
  <link rel="preload" href="/codeglyphx-qr-icon.png" as="image" type="image/png" />
  <link rel="stylesheet" href="$CssPath" />

  <!-- Open Graph -->
  <meta property="og:title" content="FAQ - CodeGlyphX" />
  <meta property="og:description" content="$($faqData.description)" />
  <meta property="og:type" content="website" />
  <meta property="og:url" content="https://codeglyphx.com/faq/" />
  <meta property="og:image" content="https://codeglyphx.com/codeglyphx-qr-icon.png" />
  <meta property="og:site_name" content="CodeGlyphX" />

  <!-- Twitter Card -->
  <meta name="twitter:card" content="summary" />
  <meta name="twitter:title" content="FAQ - CodeGlyphX" />
  <meta name="twitter:description" content="$($faqData.description)" />
  <meta name="twitter:image" content="https://codeglyphx.com/codeglyphx-qr-icon.png" />

$jsonLdScript
$breadcrumbJsonLd
  <script>(function(){var theme=localStorage.getItem('theme')||'dark';document.documentElement.setAttribute('data-theme',theme);})();</script>
</head>
<body$faqBodyClassAttr>
  <div class="page">
$header
<main>
$faqContent
</main>
$footer
  </div>
  <script src="/js/site.js"></script>
  <script>
    // Theme toggle
    document.querySelectorAll('.theme-toggle').forEach(function(btn) {
      btn.addEventListener('click', function() {
        var current = document.documentElement.getAttribute('data-theme') || 'dark';
        var next = current === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-theme', next);
        localStorage.setItem('theme', next);
      });
    });
    // Keyboard focus visibility
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
  </script>
</body>
</html>
"@

    $faqOutputFile = Join-Path $faqDir "index.html"
    Set-Content -Path $faqOutputFile -Value $faqHtml -Encoding UTF8
    Write-Host "  Generated: $faqOutputFile" -ForegroundColor Green
}

# ============================================================================
# SHOWCASE PAGE (Generated from JSON)
# ============================================================================
Write-Host "Generating Showcase page from JSON..." -ForegroundColor Cyan

$showcaseJsonPath = Join-Path $repoRoot "Assets" "Data" "showcase.json"
if (-not (Test-Path $showcaseJsonPath)) {
    Write-Warning "Showcase JSON not found at $showcaseJsonPath - skipping Showcase generation"
} else {
    $showcaseData = Get-Content $showcaseJsonPath -Raw | ConvertFrom-Json

    # Icon SVGs
    $icons = @{
        info = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>'
        lock = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"/></svg>'
        check = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16"><path d="M5 13l4 4L19 7"/></svg>'
        github = '<svg viewBox="0 0 24 24" fill="currentColor" class="btn-icon"><path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/></svg>'
        download = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="btn-icon"><path d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/></svg>'
        prev = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M15 19l-7-7 7-7"/></svg>'
        next = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M9 5l7 7-7 7"/></svg>'
        plus = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="18" height="18"><path d="M12 5v14M5 12h14"/></svg>'
        shield = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/></svg>'
    }

    $showcaseHtml = [System.Text.StringBuilder]::new()
    [void]$showcaseHtml.AppendLine('<div class="showcase-page">')
    [void]$showcaseHtml.AppendLine('    <div class="showcase-hero">')
    [void]$showcaseHtml.AppendLine('        <span class="section-label">Built with CodeGlyphX</span>')
    [void]$showcaseHtml.AppendLine("        <h1>$($showcaseData.title)</h1>")
    [void]$showcaseHtml.AppendLine("        <p>$($showcaseData.description)</p>")
    [void]$showcaseHtml.AppendLine('    </div>')
    [void]$showcaseHtml.AppendLine('')
    [void]$showcaseHtml.AppendLine('    <div class="showcase-grid">')

    $itemIndex = 0
    foreach ($item in $showcaseData.items) {
        $carouselId = "carousel-$($item.id)"
        $iconSvg = $icons[$item.icon]

        [void]$showcaseHtml.AppendLine('        <!-- ' + $item.name + ' Card -->')
        [void]$showcaseHtml.AppendLine('        <div class="showcase-card showcase-card-large">')
        [void]$showcaseHtml.AppendLine('            <div class="showcase-header">')
        [void]$showcaseHtml.AppendLine("                <div class=`"showcase-icon`">$iconSvg</div>")
        [void]$showcaseHtml.AppendLine('                <div class="showcase-title">')
        [void]$showcaseHtml.AppendLine("                    <h2>$($item.name)</h2>")
        [void]$showcaseHtml.AppendLine("                    <span class=`"showcase-badge`">$($item.badge)</span>")
        [void]$showcaseHtml.AppendLine('                </div>')
        [void]$showcaseHtml.AppendLine('            </div>')

        # Meta (license + tech)
        [void]$showcaseHtml.AppendLine('            <div class="showcase-meta">')
        if ($item.license) {
            [void]$showcaseHtml.AppendLine("                <span class=`"showcase-license`">$($icons['shield'])$($item.license)</span>")
        }
        foreach ($tech in $item.tech) {
            [void]$showcaseHtml.AppendLine("                <span class=`"showcase-tech`">$tech</span>")
        }
        [void]$showcaseHtml.AppendLine('            </div>')

        # Description
        [void]$showcaseHtml.AppendLine("            <p class=`"showcase-description`">$($item.description)</p>")

        # Features list
        [void]$showcaseHtml.AppendLine('            <div class="showcase-details">')
        [void]$showcaseHtml.AppendLine('                <h4>Key Features</h4>')
        [void]$showcaseHtml.AppendLine('                <ul>')
        foreach ($feature in $item.features) {
            [void]$showcaseHtml.AppendLine("                    <li>$feature</li>")
        }
        [void]$showcaseHtml.AppendLine('                </ul>')
        [void]$showcaseHtml.AppendLine('            </div>')

        # Highlights
        [void]$showcaseHtml.AppendLine('            <div class="showcase-features">')
        foreach ($highlight in $item.highlights) {
            [void]$showcaseHtml.AppendLine("                <span class=`"showcase-feature`">$($icons['check'])$highlight</span>")
        }
        [void]$showcaseHtml.AppendLine('            </div>')

        # Gallery with carousel
        [void]$showcaseHtml.AppendLine("            <div class=`"showcase-gallery`" data-carousel=`"$carouselId`">")
        [void]$showcaseHtml.AppendLine('                <div class="showcase-gallery-tabs">')
        [void]$showcaseHtml.AppendLine("                    <button class=`"showcase-gallery-tab active`" data-theme=`"dark`">Dark Theme</button>")
        [void]$showcaseHtml.AppendLine("                    <button class=`"showcase-gallery-tab`" data-theme=`"light`">Light Theme</button>")
        [void]$showcaseHtml.AppendLine('                </div>')

        [void]$showcaseHtml.AppendLine('                <div class="showcase-carousel">')
        [void]$showcaseHtml.AppendLine('                    <div class="showcase-carousel-viewport">')

        # Dark theme images (shown by default)
        $slideIdx = 0
        foreach ($img in $item.images.dark) {
            $activeClass = if ($slideIdx -eq 0) { " active" } else { "" }
            [void]$showcaseHtml.AppendLine("                        <div class=`"showcase-carousel-slide$activeClass`" data-theme=`"dark`" data-index=`"$slideIdx`">")
            [void]$showcaseHtml.AppendLine("                            <img src=`"$($img.src)`" alt=`"$($img.alt)`" loading=`"lazy`" />")
            [void]$showcaseHtml.AppendLine('                        </div>')
            $slideIdx++
        }

        # Light theme images (hidden by default)
        $slideIdx = 0
        foreach ($img in $item.images.light) {
            [void]$showcaseHtml.AppendLine("                        <div class=`"showcase-carousel-slide`" data-theme=`"light`" data-index=`"$slideIdx`" style=`"display:none`">")
            [void]$showcaseHtml.AppendLine("                            <img src=`"$($img.src)`" alt=`"$($img.alt)`" loading=`"lazy`" />")
            [void]$showcaseHtml.AppendLine('                        </div>')
            $slideIdx++
        }

        [void]$showcaseHtml.AppendLine("                        <button class=`"showcase-carousel-nav prev`" aria-label=`"Previous image`">$($icons['prev'])</button>")
        [void]$showcaseHtml.AppendLine("                        <button class=`"showcase-carousel-nav next`" aria-label=`"Next image`">$($icons['next'])</button>")
        [void]$showcaseHtml.AppendLine('                    </div>')

        # Footer with caption, dots, counter
        [void]$showcaseHtml.AppendLine('                    <div class="showcase-carousel-footer">')
        [void]$showcaseHtml.AppendLine("                        <span class=`"showcase-carousel-caption`">$($item.images.dark[0].caption)</span>")
        [void]$showcaseHtml.AppendLine('                        <div class="showcase-carousel-dots">')
        for ($i = 0; $i -lt $item.images.dark.Count; $i++) {
            $dotActive = if ($i -eq 0) { " active" } else { "" }
            [void]$showcaseHtml.AppendLine("                            <button class=`"showcase-carousel-dot$dotActive`" data-index=`"$i`" aria-label=`"Go to slide $($i + 1)`"></button>")
        }
        [void]$showcaseHtml.AppendLine('                        </div>')
        [void]$showcaseHtml.AppendLine("                        <span class=`"showcase-carousel-counter`">1 / $($item.images.dark.Count)</span>")
        [void]$showcaseHtml.AppendLine('                    </div>')

        # Thumbnails
        [void]$showcaseHtml.AppendLine('                    <div class="showcase-carousel-thumbs" data-theme-container="dark">')
        $thumbIdx = 0
        foreach ($img in $item.images.dark) {
            $thumbActive = if ($thumbIdx -eq 0) { " active" } else { "" }
            [void]$showcaseHtml.AppendLine("                        <button class=`"showcase-carousel-thumb$thumbActive`" data-index=`"$thumbIdx`" aria-label=`"$($img.caption)`">")
            [void]$showcaseHtml.AppendLine("                            <img src=`"$($img.src)`" alt=`"`" loading=`"lazy`" />")
            [void]$showcaseHtml.AppendLine('                        </button>')
            $thumbIdx++
        }
        [void]$showcaseHtml.AppendLine('                    </div>')
        [void]$showcaseHtml.AppendLine('                    <div class="showcase-carousel-thumbs" data-theme-container="light" style="display:none">')
        $thumbIdx = 0
        foreach ($img in $item.images.light) {
            $thumbActive = if ($thumbIdx -eq 0) { " active" } else { "" }
            [void]$showcaseHtml.AppendLine("                        <button class=`"showcase-carousel-thumb$thumbActive`" data-index=`"$thumbIdx`" aria-label=`"$($img.caption)`">")
            [void]$showcaseHtml.AppendLine("                            <img src=`"$($img.src)`" alt=`"`" loading=`"lazy`" />")
            [void]$showcaseHtml.AppendLine('                        </button>')
            $thumbIdx++
        }
        [void]$showcaseHtml.AppendLine('                    </div>')

        [void]$showcaseHtml.AppendLine('                </div>')
        [void]$showcaseHtml.AppendLine('            </div>')

        # Store captions as data attributes for JS
        $darkCaptions = ($item.images.dark | ForEach-Object { $_.caption }) -join '|'
        $lightCaptions = ($item.images.light | ForEach-Object { $_.caption }) -join '|'
        [void]$showcaseHtml.AppendLine("            <script type=`"application/json`" class=`"carousel-data`" data-carousel=`"$carouselId`">")
        [void]$showcaseHtml.AppendLine('            {')
        [void]$showcaseHtml.AppendLine("                `"dark`": [$(($item.images.dark | ForEach-Object { '\"' + $_.caption + '\"' }) -join ', ')],")
        [void]$showcaseHtml.AppendLine("                `"light`": [$(($item.images.light | ForEach-Object { '\"' + $_.caption + '\"' }) -join ', ')]")
        [void]$showcaseHtml.AppendLine('            }')
        [void]$showcaseHtml.AppendLine('            </script>')

        # Actions
        [void]$showcaseHtml.AppendLine('            <div class="showcase-actions">')
        if ($item.github) {
            [void]$showcaseHtml.AppendLine("                <a href=`"$($item.github)`" target=`"_blank`" rel=`"noopener`" class=`"btn btn-secondary`">$($icons['github'])View on GitHub</a>")
        }
        if ($item.download) {
            [void]$showcaseHtml.AppendLine("                <a href=`"$($item.download)`" target=`"_blank`" rel=`"noopener`" class=`"btn btn-outline`">$($icons['download'])Download</a>")
        }
        if ($item.status -eq "released") {
            [void]$showcaseHtml.AppendLine('                <span class="showcase-status"><span class="status-dot" style="background: var(--success);"></span>Released</span>')
        } else {
            [void]$showcaseHtml.AppendLine('                <span class="showcase-status"><span class="status-dot"></span>In Development</span>')
        }
        [void]$showcaseHtml.AppendLine('            </div>')

        [void]$showcaseHtml.AppendLine('        </div>')
        $itemIndex++
    }

    [void]$showcaseHtml.AppendLine('    </div>')

    # Submit section
    [void]$showcaseHtml.AppendLine('    <div class="showcase-submit">')
    [void]$showcaseHtml.AppendLine('        <div class="showcase-submit-content">')
    [void]$showcaseHtml.AppendLine("            <h3>$($showcaseData.submit.title)</h3>")
    [void]$showcaseHtml.AppendLine("            <p>$($showcaseData.submit.description)</p>")
    [void]$showcaseHtml.AppendLine("            <a href=`"$($showcaseData.submit.link)`" target=`"_blank`" rel=`"noopener`" class=`"btn btn-primary`">$($icons['plus'])Submit Your Project</a>")
    [void]$showcaseHtml.AppendLine('        </div>')
    [void]$showcaseHtml.AppendLine('    </div>')
    [void]$showcaseHtml.AppendLine('</div>')

    $showcaseContent = $showcaseHtml.ToString()

    # Create showcase directory
    $showcaseDir = Join-Path $OutputPath "showcase"
    if (-not (Test-Path $showcaseDir)) {
        New-Item -ItemType Directory -Path $showcaseDir -Force | Out-Null
    }

    # BreadcrumbList JSON-LD for Showcase
    $showcaseBreadcrumbJsonLd = @"
    <script type="application/ld+json">
    {
        "@context": "https://schema.org",
        "@type": "BreadcrumbList",
        "itemListElement": [
            {
                "@type": "ListItem",
                "position": 1,
                "name": "Home",
                "item": "https://codeglyphx.com/"
            },
            {
                "@type": "ListItem",
                "position": 2,
                "name": "Showcase",
                "item": "https://codeglyphx.com/showcase/"
            }
        ]
    }
    </script>
"@

    # Carousel JavaScript
    $carouselJs = @"
    // Showcase carousel functionality
    document.querySelectorAll('.showcase-gallery').forEach(function(gallery) {
        var carouselId = gallery.dataset.carousel;
        var dataScript = document.querySelector('script.carousel-data[data-carousel="' + carouselId + '"]');
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
            captionEl.textContent = themeCaptions[currentSlide];
            counterEl.textContent = (currentSlide + 1) + ' / ' + totalSlides;
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
        prevBtn.addEventListener('click', function() { goToSlide(currentSlide - 1); });
        nextBtn.addEventListener('click', function() { goToSlide(currentSlide + 1); });

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
"@

    # Custom Showcase page with carousel JS
    $showcaseHtmlFull = @"
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Showcase - CodeGlyphX</title>
  <meta name="description" content="$($showcaseData.description)" />
  <link rel="canonical" href="https://codeglyphx.com/showcase/" />
  <link rel="icon" type="image/png" href="/codeglyphx-qr-icon.png" />
  <link rel="apple-touch-icon" href="/codeglyphx-qr-icon.png" />
  <link rel="preconnect" href="https://img.shields.io" crossorigin />
  <link rel="preload" href="/codeglyphx-qr-icon.png" as="image" type="image/png" />
  <link rel="stylesheet" href="$CssPath" />

  <!-- Open Graph -->
  <meta property="og:title" content="Showcase - CodeGlyphX" />
  <meta property="og:description" content="$($showcaseData.description)" />
  <meta property="og:type" content="website" />
  <meta property="og:url" content="https://codeglyphx.com/showcase/" />
  <meta property="og:image" content="https://codeglyphx.com/codeglyphx-qr-icon.png" />
  <meta property="og:site_name" content="CodeGlyphX" />

  <!-- Twitter Card -->
  <meta name="twitter:card" content="summary" />
  <meta name="twitter:title" content="Showcase - CodeGlyphX" />
  <meta name="twitter:description" content="$($showcaseData.description)" />
  <meta name="twitter:image" content="https://codeglyphx.com/codeglyphx-qr-icon.png" />

$showcaseBreadcrumbJsonLd
  <script>(function(){var theme=localStorage.getItem('theme')||'dark';document.documentElement.setAttribute('data-theme',theme);})();</script>
</head>
<body>
  <div class="page">
$header
<main>
$showcaseContent
</main>
$footer
  </div>
  <script src="/js/site.js"></script>
  <script>
    // Theme toggle
    document.querySelectorAll('.theme-toggle').forEach(function(btn) {
      btn.addEventListener('click', function() {
        var current = document.documentElement.getAttribute('data-theme') || 'dark';
        var next = current === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-theme', next);
        localStorage.setItem('theme', next);
      });
    });
    // Keyboard focus visibility
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
$carouselJs
  </script>
</body>
</html>
"@

    $showcaseOutputFile = Join-Path $showcaseDir "index.html"
    Set-Content -Path $showcaseOutputFile -Value $showcaseHtmlFull -Encoding UTF8
    Write-Host "  Generated: $showcaseOutputFile" -ForegroundColor Green
}

Write-Host "Static pages generated successfully!" -ForegroundColor Green
