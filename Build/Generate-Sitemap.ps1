param(
    [string]$SiteRoot,
    [string]$SiteBase = "https://codeglyphx.com"
)

$ErrorActionPreference = "Stop"

if (-not $SiteRoot) {
    $SiteRoot = Join-Path $PSScriptRoot ".." "CodeGlyphX.Website" "wwwroot"
}

$SiteBase = $SiteBase.TrimEnd('/')
$apiSitemap = Join-Path $SiteRoot "api" "sitemap.xml"
$target = Join-Path $SiteRoot "sitemap.xml"

# Get current date for lastmod
$today = (Get-Date).ToString("yyyy-MM-dd")

# Define pages with priority and change frequency
$pages = @(
    @{ url = "/"; priority = "1.0"; changefreq = "weekly" }
    @{ url = "/playground/"; priority = "0.9"; changefreq = "weekly" }
    @{ url = "/docs/"; priority = "0.9"; changefreq = "weekly" }
    @{ url = "/faq/"; priority = "0.8"; changefreq = "monthly" }
    @{ url = "/showcase/"; priority = "0.7"; changefreq = "monthly" }
    @{ url = "/api/"; priority = "0.8"; changefreq = "weekly" }
    @{ url = "/llms.txt"; priority = "0.3"; changefreq = "monthly" }
    @{ url = "/llms.json"; priority = "0.3"; changefreq = "monthly" }
    @{ url = "/llms-full.txt"; priority = "0.4"; changefreq = "monthly" }
)

# Build sitemap entries
$entries = @()
foreach ($page in $pages) {
    $entries += @"
  <url>
    <loc>$SiteBase$($page.url)</loc>
    <lastmod>$today</lastmod>
    <changefreq>$($page.changefreq)</changefreq>
    <priority>$($page.priority)</priority>
  </url>
"@
}

# Add API type pages from api/sitemap.xml if it exists
if (Test-Path $apiSitemap) {
    try {
        [xml]$xml = Get-Content $apiSitemap -Raw
        if ($xml.urlset -and $xml.urlset.url) {
            foreach ($u in $xml.urlset.url) {
                if ($u.loc) {
                    # Convert localhost URLs to production
                    $loc = [string]$u.loc
                    $loc = $loc -replace 'http://localhost:\d+', $SiteBase
                    $loc = $loc -replace 'https://localhost:\d+', $SiteBase

                    $entries += @"
  <url>
    <loc>$loc</loc>
    <lastmod>$today</lastmod>
    <changefreq>monthly</changefreq>
    <priority>0.5</priority>
  </url>
"@
                }
            }
        }
    } catch {
        Write-Warning "Could not parse API sitemap: $_"
    }
}

# Build final sitemap
$sitemap = @"
<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
$($entries -join "`n")
</urlset>
"@

$sitemap | Set-Content -Path $target -Encoding UTF8
Write-Host "Generated sitemap.xml with $($entries.Count) URLs" -ForegroundColor Green
