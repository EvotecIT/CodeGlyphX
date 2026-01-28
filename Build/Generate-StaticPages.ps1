param(
    [string]$OutputPath,
    [string]$CssPath,
    [switch]$Force
)

# Default CSS path (app.css contains all common styles)
if (-not $CssPath) {
    $CssPath = "/css/app.css"
}

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
if (-not $OutputPath) {
    # Default to 'site' folder to avoid overwriting Blazor's wwwroot/index.html
    $OutputPath = Join-Path $repoRoot "site"
    Write-Warning "No -OutputPath specified. Using default: $OutputPath"
}

function Resolve-FullPath {
    param(
        [string]$Path,
        [string]$BasePath
    )
    if ([IO.Path]::IsPathRooted($Path)) {
        return [IO.Path]::GetFullPath($Path)
    }
    return [IO.Path]::GetFullPath((Join-Path $BasePath $Path))
}

$wwwrootPath = Resolve-FullPath -Path (Join-Path $repoRoot "CodeGlyphX.Website" "wwwroot") -BasePath $repoRoot
$outputFullPath = Resolve-FullPath -Path $OutputPath -BasePath $repoRoot
if (-not $Force -and $outputFullPath.StartsWith($wwwrootPath, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to write static pages into '$wwwrootPath'. Use -Force to overwrite Blazor wwwroot assets."
}

# Paths
$assetsPath = Join-Path $repoRoot "Assets"
$templatesPath = Join-Path $assetsPath "Templates"
$scriptsPath = Join-Path $assetsPath "Scripts"
$dataPath = Join-Path $assetsPath "Data"
$fragmentsPath = [IO.Path]::Combine($repoRoot, "CodeGlyphX.Website", "wwwroot", "api-fragments")

# Load templates and fragments
$baseTemplate = Get-Content (Join-Path $templatesPath "base.html") -Raw
$header = Get-Content (Join-Path $fragmentsPath "header.html") -Raw
$footer = Get-Content (Join-Path $fragmentsPath "footer.html") -Raw

# Load scripts
$commonJs = Get-Content (Join-Path $scriptsPath "common.js") -Raw
$docsSidebarJs = Get-Content (Join-Path $scriptsPath "docs-sidebar.js") -Raw
$carouselJs = Get-Content (Join-Path $scriptsPath "carousel.js") -Raw
$benchmarkJs = Get-Content (Join-Path $scriptsPath "benchmark.js") -Raw
$styleBoardJs = Get-Content (Join-Path $scriptsPath "style-board.js") -Raw

# Icon definitions for reuse
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

function New-StaticPage {
    param(
        [string]$Title,
        [string]$Description,
        [string]$Content,
        [string]$OutputFile,
        [string]$BodyClass = "",
        [string]$ExtraCss = "",
        [string]$Canonical = "",
        [string]$OpenGraph = "",
        [string]$StructuredData = "",
        [string]$ExtraScripts = "",
        [string]$PageCssPath = ""
    )

    # Use page-specific CSS if provided, otherwise use script-level default
    $effectiveCssPath = if ($PageCssPath) { $PageCssPath } else { $CssPath }

    $bodyClassAttr = if ($BodyClass) { " class=`"$BodyClass`"" } else { "" }
    $extraCssLink = if ($ExtraCss) { "`n  <link rel=`"stylesheet`" href=`"$ExtraCss`" />" } else { "" }
    $canonicalLink = if ($Canonical) { "`n  <link rel=`"canonical`" href=`"$Canonical`" />" } else { "" }

    # Build scripts block
    $scriptsBlock = @"
  <script>
$commonJs
  </script>
"@
    if ($ExtraScripts) {
        $scriptsBlock += "`n  <script>`n$ExtraScripts`n  </script>"
    }

    # Apply template substitutions
    $html = $baseTemplate
    $html = $html -replace '{{TITLE}}', $Title
    $html = $html -replace '{{DESCRIPTION}}', $Description
    $html = $html -replace '{{CSS_PATH}}', $effectiveCssPath
    $html = $html -replace '{{EXTRA_CSS}}', $extraCssLink
    $html = $html -replace '{{CANONICAL}}', $canonicalLink
    $html = $html -replace '{{OPENGRAPH}}', $OpenGraph
    $html = $html -replace '{{STRUCTURED_DATA}}', $StructuredData
    $html = $html -replace '{{BODY_CLASS}}', $bodyClassAttr
    $html = $html -replace '{{HEADER}}', $header
    $html = $html -replace '{{CONTENT}}', $Content
    $html = $html -replace '{{FOOTER}}', $footer
    $html = $html -replace '{{EXTRA_SCRIPTS}}', $scriptsBlock

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

$homeContent = Get-Content ([IO.Path]::Combine($templatesPath, "pages", "home.html")) -Raw

New-StaticPage `
    -Title "CodeGlyphX - Zero-Dependency QR & Barcode Toolkit for .NET" `
    -Description "CodeGlyphX is a blazing-fast, zero-dependency .NET library for generating and decoding QR codes, barcodes, Data Matrix, PDF417, and Aztec codes." `
    -Content $homeContent `
    -OutputFile (Join-Path $OutputPath "index.html") `
    -ExtraScripts $styleBoardJs

# ============================================================================
# DOCS PAGE
# ============================================================================
Write-Host "Generating Docs page..." -ForegroundColor Cyan

$docsContent = Get-Content ([IO.Path]::Combine($templatesPath, "pages", "docs.html")) -Raw
$docsDir = Join-Path $OutputPath "docs"
if (-not (Test-Path $docsDir)) {
    New-Item -ItemType Directory -Path $docsDir -Force | Out-Null
}

New-StaticPage `
    -Title "Documentation - CodeGlyphX" `
    -Description "CodeGlyphX documentation - learn how to generate and decode QR codes, barcodes, and 2D matrix codes in .NET." `
    -Content $docsContent `
    -OutputFile (Join-Path $docsDir "index.html") `
    -PageCssPath "/css/api-docs.css" `
    -ExtraScripts $docsSidebarJs

# ============================================================================
# FAQ PAGE (Generated from JSON)
# ============================================================================
Write-Host "Generating FAQ page from JSON..." -ForegroundColor Cyan

$faqJsonPath = Join-Path $dataPath "faq.json"
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
            $plainAnswer = $item.answer -replace '<[^>]+>', ' ' -replace '\s+', ' '
            $plainAnswer = $plainAnswer.Trim()
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

    $faqJsonLd = @"

  <script type="application/ld+json">
  {
      "@context": "https://schema.org",
      "@type": "FAQPage",
      "mainEntity": [
$($jsonLdQuestions -join ",`n")
      ]
  }
  </script>
  <script type="application/ld+json">
  {
      "@context": "https://schema.org",
      "@type": "BreadcrumbList",
      "itemListElement": [
          { "@type": "ListItem", "position": 1, "name": "Home", "item": "https://codeglyphx.com/" },
          { "@type": "ListItem", "position": 2, "name": "FAQ", "item": "https://codeglyphx.com/faq/" }
      ]
  }
  </script>
"@

    $faqOpenGraph = @"

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
"@

    $faqDir = Join-Path $OutputPath "faq"
    if (-not (Test-Path $faqDir)) {
        New-Item -ItemType Directory -Path $faqDir -Force | Out-Null
    }

    New-StaticPage `
        -Title "FAQ - CodeGlyphX" `
        -Description $faqData.description `
        -Content $faqHtmlBuilder.ToString() `
        -OutputFile (Join-Path $faqDir "index.html") `
        -Canonical "https://codeglyphx.com/faq/" `
        -OpenGraph $faqOpenGraph `
        -StructuredData $faqJsonLd
}

# ============================================================================
# SHOWCASE PAGE (Generated from JSON)
# ============================================================================
Write-Host "Generating Showcase page from JSON..." -ForegroundColor Cyan

$showcaseJsonPath = Join-Path $dataPath "showcase.json"
if (-not (Test-Path $showcaseJsonPath)) {
    Write-Warning "Showcase JSON not found at $showcaseJsonPath - skipping Showcase generation"
} else {
    $showcaseData = Get-Content $showcaseJsonPath -Raw | ConvertFrom-Json

    $showcaseHtml = [System.Text.StringBuilder]::new()
    [void]$showcaseHtml.AppendLine('<div class="showcase-page">')
    [void]$showcaseHtml.AppendLine('    <div class="showcase-hero">')
    [void]$showcaseHtml.AppendLine('        <span class="section-label">Built with CodeGlyphX</span>')
    [void]$showcaseHtml.AppendLine("        <h1>$($showcaseData.title)</h1>")
    [void]$showcaseHtml.AppendLine("        <p>$($showcaseData.description)</p>")
    [void]$showcaseHtml.AppendLine('    </div>')
    [void]$showcaseHtml.AppendLine('')
    [void]$showcaseHtml.AppendLine('    <div class="showcase-grid">')

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

        # Dark theme images
        $slideIdx = 0
        foreach ($img in $item.images.dark) {
            $activeClass = if ($slideIdx -eq 0) { " active" } else { "" }
            [void]$showcaseHtml.AppendLine("                        <div class=`"showcase-carousel-slide$activeClass`" data-theme=`"dark`" data-index=`"$slideIdx`">")
            [void]$showcaseHtml.AppendLine("                            <img src=`"$($img.src)`" alt=`"$($img.alt)`" loading=`"lazy`" />")
            [void]$showcaseHtml.AppendLine('                        </div>')
            $slideIdx++
        }

        # Light theme images
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

        # Footer
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

        # Carousel data
        $carouselJson = @{
            dark  = @($item.images.dark | ForEach-Object { $_.caption })
            light = @($item.images.light | ForEach-Object { $_.caption })
        } | ConvertTo-Json -Compress -Depth 4
        [void]$showcaseHtml.AppendLine("            <script type=`"application/json`" class=`"carousel-data`" data-carousel=`"$carouselId`">")
        [void]$showcaseHtml.AppendLine("            $carouselJson")
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

    $showcaseBreadcrumbJsonLd = @"

  <script type="application/ld+json">
  {
      "@context": "https://schema.org",
      "@type": "BreadcrumbList",
      "itemListElement": [
          { "@type": "ListItem", "position": 1, "name": "Home", "item": "https://codeglyphx.com/" },
          { "@type": "ListItem", "position": 2, "name": "Showcase", "item": "https://codeglyphx.com/showcase/" }
      ]
  }
  </script>
"@

    $showcaseOpenGraph = @"

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
"@

    $showcaseDir = Join-Path $OutputPath "showcase"
    if (-not (Test-Path $showcaseDir)) {
        New-Item -ItemType Directory -Path $showcaseDir -Force | Out-Null
    }

    New-StaticPage `
        -Title "Showcase - CodeGlyphX" `
        -Description $showcaseData.description `
        -Content $showcaseHtml.ToString() `
        -OutputFile (Join-Path $showcaseDir "index.html") `
        -Canonical "https://codeglyphx.com/showcase/" `
        -OpenGraph $showcaseOpenGraph `
        -StructuredData $showcaseBreadcrumbJsonLd `
        -ExtraScripts $carouselJs
}

# ============================================================================
# PRICING PAGE
# ============================================================================
Write-Host "Generating Pricing page..." -ForegroundColor Cyan

$pricingContent = @"
<section class="pricing-page">
    <div class="pricing-hero">
        <span class="section-label">Pricing</span>
        <h1>Simple, Transparent Pricing</h1>
        <p>CodeGlyphX is free and open source. Sponsorships support ongoing development and give you a voice in the project's direction.</p>
    </div>

    <div class="pricing-grid">
        <!-- Free Tier -->
        <div class="pricing-card">
            <div class="pricing-card-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true" focusable="false">
                    <path d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
                </svg>
            </div>
            <h2>Free</h2>
            <div class="pricing-tag">Open Source</div>
            <div class="pricing-amount">
                <span class="pricing-currency">`$0</span>
                <span class="pricing-period">forever</span>
            </div>
            <p class="pricing-desc">Everything you need. No limits, no paywalls, no feature gating.</p>
            <ul class="pricing-features">
                <li>All current features included</li>
                <li>All future features included</li>
                <li>No usage limits</li>
                <li>Apache 2.0 license</li>
                <li>Community support via GitHub and Discord</li>
                <li>Full source code access</li>
            </ul>
            <a href="https://www.nuget.org/packages/CodeGlyphX" target="_blank" rel="noopener" class="pricing-btn pricing-btn-secondary">
                Install from NuGet
            </a>
        </div>

        <!-- Sponsor Tier -->
        <div class="pricing-card pricing-card-featured">
            <div class="pricing-card-badge">Recommended</div>
            <div class="pricing-card-icon pricing-icon-star">
                <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true" focusable="false">
                    <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
                </svg>
            </div>
            <h2>Sponsor</h2>
            <div class="pricing-tag">GitHub Sponsors</div>
            <div class="pricing-amount">
                <span class="pricing-currency pricing-highlight">Flexible</span>
            </div>
            <p class="pricing-desc">Support ongoing development and get a voice in the project's direction.</p>
            <ul class="pricing-features">
                <li>Everything in Free</li>
                <li>Priority feature requests</li>
                <li>Priority bug reports</li>
                <li>Recognition on project page</li>
                <li>Help shape the roadmap</li>
                <li>Direct support channel</li>
            </ul>
            <a href="https://github.com/sponsors/PrzemyslawKlys" target="_blank" rel="noopener" class="pricing-btn pricing-btn-primary">
                <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true" focusable="false" style="width:16px;height:16px;">
                    <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/>
                </svg>
                Become a Sponsor
            </a>
        </div>

        <!-- Donation Tier -->
        <div class="pricing-card">
            <div class="pricing-card-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true" focusable="false">
                    <path d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z"/>
                </svg>
            </div>
            <h2>Donate</h2>
            <div class="pricing-tag">One-Time or Recurring</div>
            <div class="pricing-amount">
                <span class="pricing-currency">Any Amount</span>
            </div>
            <p class="pricing-desc">Prefer PayPal or need an invoice? We can accommodate businesses and individuals.</p>
            <ul class="pricing-features">
                <li>Everything in Free</li>
                <li>One-time or recurring donations</li>
                <li>PayPal transfers accepted</li>
                <li>Invoices available on request</li>
                <li>Corporate sponsorship options</li>
                <li>Recognition on project page</li>
            </ul>
            <a href="https://paypal.me/PrzemyslawKlys" target="_blank" rel="noopener" class="pricing-btn pricing-btn-secondary">
                Donate via PayPal
            </a>
        </div>
    </div>

    <div class="pricing-note">
        <h3>Why Support Open Source?</h3>
        <p>
            CodeGlyphX is built and maintained by
            <a href="https://twitter.com/PrzemyslawKlys" target="_blank">Przemys&#x142;aw K&#x142;ys</a>
            at <a href="https://evotec.xyz" target="_blank">Evotec Services sp. z o.o.</a>
            Your sponsorship directly funds development time, testing infrastructure, and documentation.
            Every contribution helps keep the project actively maintained and growing.
        </p>
        <p>
            <strong>Need an invoice?</strong> Contact us at
            <a href="mailto:contact@evotec.pl">contact@evotec.pl</a>
            &mdash; we can provide VAT invoices for corporate sponsors and business donations.
        </p>
    </div>
</section>
"@

$pricingOpenGraph = @"

  <!-- Open Graph -->
  <meta property="og:title" content="Pricing - CodeGlyphX" />
  <meta property="og:description" content="CodeGlyphX is free and open source. Sponsorships support ongoing development." />
  <meta property="og:type" content="website" />
  <meta property="og:url" content="https://codeglyphx.com/pricing/" />
  <meta property="og:image" content="https://codeglyphx.com/codeglyphx-qr-icon.png" />
  <meta property="og:site_name" content="CodeGlyphX" />

  <!-- Twitter Card -->
  <meta name="twitter:card" content="summary" />
  <meta name="twitter:title" content="Pricing - CodeGlyphX" />
  <meta name="twitter:description" content="CodeGlyphX is free and open source. Sponsorships support ongoing development." />
  <meta name="twitter:image" content="https://codeglyphx.com/codeglyphx-qr-icon.png" />
"@

$pricingBreadcrumbJsonLd = @"

  <script type="application/ld+json">
  {
      "@context": "https://schema.org",
      "@type": "BreadcrumbList",
      "itemListElement": [
          { "@type": "ListItem", "position": 1, "name": "Home", "item": "https://codeglyphx.com/" },
          { "@type": "ListItem", "position": 2, "name": "Pricing", "item": "https://codeglyphx.com/pricing/" }
      ]
  }
  </script>
"@

$pricingDir = Join-Path $OutputPath "pricing"
if (-not (Test-Path $pricingDir)) {
    New-Item -ItemType Directory -Path $pricingDir -Force | Out-Null
}

New-StaticPage `
    -Title "Pricing - CodeGlyphX" `
    -Description "CodeGlyphX is free and open source. Sponsorships support ongoing development and give you a voice in the project's direction." `
    -Content $pricingContent `
    -OutputFile (Join-Path $pricingDir "index.html") `
    -Canonical "https://codeglyphx.com/pricing/" `
    -OpenGraph $pricingOpenGraph `
    -StructuredData $pricingBreadcrumbJsonLd

# ============================================================================
# BENCHMARKS PAGE
# ============================================================================
Write-Host "Generating Benchmarks page..." -ForegroundColor Cyan

$benchmarkContent = Get-Content ([IO.Path]::Combine($templatesPath, "pages", "benchmark.html")) -Raw

$benchmarkOpenGraph = @"

  <!-- Open Graph -->
  <meta property="og:title" content="Benchmarks - CodeGlyphX" />
  <meta property="og:description" content="Performance benchmarks comparing CodeGlyphX with other .NET barcode libraries." />
  <meta property="og:type" content="website" />
  <meta property="og:url" content="https://codeglyphx.com/benchmarks/" />
  <meta property="og:image" content="https://codeglyphx.com/codeglyphx-qr-icon.png" />
  <meta property="og:site_name" content="CodeGlyphX" />

  <!-- Twitter Card -->
  <meta name="twitter:card" content="summary" />
  <meta name="twitter:title" content="Benchmarks - CodeGlyphX" />
  <meta name="twitter:description" content="Performance benchmarks comparing CodeGlyphX with other .NET barcode libraries." />
  <meta name="twitter:image" content="https://codeglyphx.com/codeglyphx-qr-icon.png" />
"@

$benchmarkBreadcrumbJsonLd = @"

  <script type="application/ld+json">
  {
      "@context": "https://schema.org",
      "@type": "BreadcrumbList",
      "itemListElement": [
          { "@type": "ListItem", "position": 1, "name": "Home", "item": "https://codeglyphx.com/" },
          { "@type": "ListItem", "position": 2, "name": "Benchmarks", "item": "https://codeglyphx.com/benchmarks/" }
      ]
  }
  </script>
"@

$benchmarkDir = Join-Path $OutputPath "benchmarks"
if (-not (Test-Path $benchmarkDir)) {
    New-Item -ItemType Directory -Path $benchmarkDir -Force | Out-Null
}

New-StaticPage `
    -Title "Benchmarks - CodeGlyphX" `
    -Description "Performance benchmarks comparing CodeGlyphX with other .NET barcode libraries. Transparent methodology and raw data." `
    -Content $benchmarkContent `
    -OutputFile (Join-Path $benchmarkDir "index.html") `
    -Canonical "https://codeglyphx.com/benchmarks/" `
    -OpenGraph $benchmarkOpenGraph `
    -StructuredData $benchmarkBreadcrumbJsonLd `
    -ExtraScripts $benchmarkJs

Write-Host "Static pages generated successfully!" -ForegroundColor Green
