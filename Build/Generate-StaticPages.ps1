param(
    [string]$OutputPath,
    [string]$CssPath,
    [switch]$Force
)

# Default CSS path
if (-not $CssPath) {
    $CssPath = "/css/api-docs.css"
}

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
if (-not $OutputPath) {
    $OutputPath = [IO.Path]::Combine($repoRoot, "CodeGlyphX.Website", "wwwroot")
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
        [string]$ExtraScripts = ""
    )

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
    $html = $html -replace '{{CSS_PATH}}', $CssPath
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
    -OutputFile (Join-Path $OutputPath "index.html")

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

Write-Host "Static pages generated successfully!" -ForegroundColor Green
