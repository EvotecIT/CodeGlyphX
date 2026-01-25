param(
    [string]$NavPath = "CodeGlyphX.Website/wwwroot/data/site-nav.json",
    [string]$HeaderPath = "CodeGlyphX.Website/wwwroot/api-fragments/header.html",
    [string]$FooterPath = "CodeGlyphX.Website/wwwroot/api-fragments/footer.html"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$navFullPath = if ([IO.Path]::IsPathRooted($NavPath)) { $NavPath } else { Join-Path $repoRoot $NavPath }
$headerFullPath = if ([IO.Path]::IsPathRooted($HeaderPath)) { $HeaderPath } else { Join-Path $repoRoot $HeaderPath }
$footerFullPath = if ([IO.Path]::IsPathRooted($FooterPath)) { $FooterPath } else { Join-Path $repoRoot $FooterPath }

if (-not (Test-Path $navFullPath)) { throw "Nav config not found at $navFullPath" }
if (-not (Test-Path $headerFullPath)) { throw "Header fragment not found at $headerFullPath" }
if (-not (Test-Path $footerFullPath)) { throw "Footer fragment not found at $footerFullPath" }

$navConfig = Get-Content -Path $navFullPath -Raw | ConvertFrom-Json
$hasPrimary = $navConfig.PSObject.Properties.Name -contains "primary"
$primaryLinks = if ($hasPrimary) { $navConfig.primary } elseif ($navConfig.links) { $navConfig.links } else { @() }
$footerConfig = $navConfig.footer

$headerContainerIndent = "            "
$headerLinkIndent = "                "
$footerContainerIndent = "                "
$footerLinkIndent = "                    "

function Format-LinkList {
    param(
        [object[]]$Links,
        [string]$Indent
    )

    if (-not $Links) { return "" }
    $lines = foreach ($link in $Links) {
        $href = $link.href
        $text = $link.text
        if (-not $href -or -not $text) { continue }
        $isExternal = $href -match '^https?://'
        $attrs = @()
        if ($isExternal) {
            $label = $text -replace '"', '&quot;'
            $attrs += 'target="_blank"'
            $attrs += 'rel="noopener"'
            $attrs += "aria-label=`"$label (opens in new tab)`""
        }
        $attrString = if ($attrs.Count -gt 0) { " " + ($attrs -join " ") } else { "" }
        "$Indent<a href=`"$href`"$attrString>$text</a>"
    }
    return ($lines -join "`n")
}

function Replace-Section {
    param(
        [string]$Html,
        [string]$SectionTitle,
        [string]$LinksHtml
    )

    if (-not $LinksHtml) { return $Html }
    $pattern = "(?s)(<div class=`"footer-section`">\\s*<h3>$([regex]::Escape($SectionTitle))</h3>)(.*?)(</div>)"
    return [regex]::Replace($Html, $pattern, {
        param($match)
        "$($match.Groups[1].Value)`n$LinksHtml`n$footerContainerIndent$($match.Groups[3].Value)"
    }, 1)
}

Write-Host "Updating navigation fragments from $navFullPath..." -ForegroundColor Cyan

$headerHtml = Get-Content -Path $headerFullPath -Raw
if ($primaryLinks.Count -gt 0) {
    $headerHtml = [regex]::Replace($headerHtml, "(?s)(<div class=`"nav-links`">)(.*?)(</div>)", {
        param($match)
        $linksHtml = Format-LinkList -Links $primaryLinks -Indent $headerLinkIndent
        "$($match.Groups[1].Value)`n$linksHtml`n$headerContainerIndent$($match.Groups[3].Value)"
    }, 1)
}

$headerHtml = $headerHtml.TrimEnd("`r", "`n") + [Environment]::NewLine
Set-Content -Path $headerFullPath -Value $headerHtml -Encoding UTF8

$footerHtml = Get-Content -Path $footerFullPath -Raw
if ($footerConfig) {
    $footerHtml = Replace-Section -Html $footerHtml -SectionTitle "Product" -LinksHtml (Format-LinkList -Links $footerConfig.product -Indent $footerLinkIndent)
    $footerHtml = Replace-Section -Html $footerHtml -SectionTitle "Resources" -LinksHtml (Format-LinkList -Links $footerConfig.resources -Indent $footerLinkIndent)
    $footerHtml = Replace-Section -Html $footerHtml -SectionTitle "Company" -LinksHtml (Format-LinkList -Links $footerConfig.company -Indent $footerLinkIndent)
}

$footerHtml = [regex]::Replace(
    $footerHtml,
    '(?i)<a\s+href="(https?://[^"]+)"(?![^>]*aria-label)([^>]*)>([^<]+)</a>',
    {
        param($match)
        $href = $match.Groups[1].Value
        $attrs = $match.Groups[2].Value
        $text = $match.Groups[3].Value
        $label = $text -replace '"', '&quot;'
        "<a href=`"$href`"$attrs aria-label=`"$label (opens in new tab)`">$text</a>"
    }
)

$footerHtml = $footerHtml.TrimEnd("`r", "`n") + [Environment]::NewLine
Set-Content -Path $footerFullPath -Value $footerHtml -Encoding UTF8

Write-Host "Navigation fragments updated." -ForegroundColor Green
