param(
    [string]$Configuration = "Release",
    [string]$Framework = "net10.0",
    [string]$WebsiteProject = "CodeGlyphX.Website/CodeGlyphX.Website.csproj",
    [string]$OutputPath = "site",
    [switch]$MinifyAssets
)

$ErrorActionPreference = "Stop"

function Invoke-Step {
    param([string]$Label, [string[]]$Command)
    Write-Host $Label -ForegroundColor Cyan
    $exe = $Command[0]
    $args = @()
    if ($Command.Length -gt 1) {
        $args = $Command[1..($Command.Length - 1)]
    }
    & $exe @args
    if ($LASTEXITCODE -ne 0) {
        throw "$Label failed with exit code $LASTEXITCODE."
    }
}

function Set-BaseHref {
    param(
        [string]$FilePath,
        [string]$BaseHref
    )
    if (-not (Test-Path $FilePath)) { return }
    $content = Get-Content -Path $FilePath -Raw
    $pattern = '<base href="/"\s*/?>'
    $updated = [System.Text.RegularExpressions.Regex]::Replace($content, $pattern, "<base href=`"$BaseHref`" />", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($updated -ne $content) {
        Set-Content -Path $FilePath -Value $updated -Encoding UTF8
    }
}

function Get-ProjectFrameworks {
    param([string]$ProjectPath)
    if (-not (Test-Path $ProjectPath)) { return @() }
    $content = Get-Content -Path $ProjectPath -Raw
    if (-not $content) { return @() }
    [xml]$proj = $content
    $frameworks = @()
    foreach ($group in $proj.Project.PropertyGroup) {
        if ($group.TargetFrameworks) {
            $frameworks += $group.TargetFrameworks -split ';'
        }
        if ($group.TargetFramework) {
            $frameworks += $group.TargetFramework
        }
    }
    return $frameworks | Where-Object { $_ -and $_.Trim().Length -gt 0 } | ForEach-Object { $_.Trim() } | Select-Object -Unique
}

function Update-BootIntegrity {
    param(
        [string]$FrameworkPath
    )
    $bootPath = Join-Path $FrameworkPath "blazor.boot.json"
    if (-not (Test-Path $bootPath)) { return }

    $boot = Get-Content -Path $bootPath -Raw | ConvertFrom-Json
    $resources = $boot.resources
    if ($null -eq $resources) { return }

    foreach ($prop in $resources.PSObject.Properties) {
        $value = $prop.Value
        if ($value -is [System.Management.Automation.PSCustomObject]) {
            foreach ($fileProp in $value.PSObject.Properties) {
                $fileName = $fileProp.Name
                $filePath = Join-Path $FrameworkPath $fileName
                if (Test-Path $filePath) {
                    $bytes = [System.IO.File]::ReadAllBytes($filePath)
                    $hash = [System.Convert]::ToBase64String([System.Security.Cryptography.SHA256]::Create().ComputeHash($bytes))
                    $fileProp.Value = "sha256-$hash"
                }
            }
        }
    }

    $boot | ConvertTo-Json -Depth 20 | Set-Content -Path $bootPath -Encoding UTF8
}

function Copy-FingerprintedBlazorJs {
    param(
        [string]$FrameworkPath
    )
    # Find the fingerprinted blazor.webassembly.*.js file and copy it to blazor.webassembly.js
    $fingerprintedFile = Get-ChildItem -Path $FrameworkPath -Filter "blazor.webassembly.*.js" |
        Where-Object { $_.Name -notmatch '\.(gz|br)$' } |
        Select-Object -First 1

    if ($fingerprintedFile) {
        $targetPath = Join-Path $FrameworkPath "blazor.webassembly.js"
        Copy-Item -Path $fingerprintedFile.FullName -Destination $targetPath -Force
        Write-Host "  Copied $($fingerprintedFile.Name) -> blazor.webassembly.js" -ForegroundColor Gray
    }
}

function Add-BlazorScriptCacheBuster {
    param(
        [string]$HtmlPath
    )
    if (-not (Test-Path $HtmlPath)) { return }
    $version = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    $content = Get-Content -Path $HtmlPath -Raw
    $pattern = 'src="_framework/blazor\.webassembly\.js"'
    $replacement = "src=`"_framework/blazor.webassembly.js?v=$version`""
    $updated = [System.Text.RegularExpressions.Regex]::Replace($content, $pattern, $replacement)
    if ($updated -ne $content) {
        Set-Content -Path $HtmlPath -Value $updated -Encoding UTF8
        Write-Host "  Added cache buster to Blazor script reference" -ForegroundColor Gray
    }
}

function Invoke-InlineCriticalCss {
    param(
        [string]$SiteRoot,
        [string]$CriticalCssPath
    )

    if (-not (Test-Path $CriticalCssPath)) {
        Write-Warning "Critical CSS not found at $CriticalCssPath - skipping inline optimization"
        return
    }

    $criticalCss = Get-Content -Path $CriticalCssPath -Raw
    # Minify critical CSS (remove comments, extra whitespace)
    $criticalCss = $criticalCss -replace '/\*[\s\S]*?\*/', ''
    $criticalCss = $criticalCss -replace '\s+', ' '
    $criticalCss = $criticalCss -replace '\s*([{};:,])\s*', '$1'
    $criticalCss = $criticalCss.Trim()

    $htmlFiles = Get-ChildItem -Path $SiteRoot -Filter "*.html" -Recurse -File

    foreach ($html in $htmlFiles) {
        $content = Get-Content -Path $html.FullName -Raw
        if (-not $content) { continue }

        # Skip if already has inlined critical CSS
        if ($content -match '<!--\s*critical-css\s*-->') { continue }

        # Find the CSS link and make it async
        # Pattern matches: <link rel="stylesheet" href="...app.css..." />
        $cssPattern = '(<link\s+rel="stylesheet"\s+href="([^"]*(?:app|api-docs)\.css[^"]*)"\s*/?>)'

        if ($content -match $cssPattern) {
            $originalLink = $Matches[1]
            $cssHref = $Matches[2]

            # Create async CSS loading with noscript fallback
            $asyncCss = @"
<!-- critical-css -->
    <style>$criticalCss</style>
    <link rel="preload" href="$cssHref" as="style" onload="this.onload=null;this.rel='stylesheet'">
    <noscript><link rel="stylesheet" href="$cssHref"></noscript>
"@
            $updated = $content -replace [regex]::Escape($originalLink), $asyncCss
            if ($updated -ne $content) {
                Set-Content -Path $html.FullName -Value $updated -Encoding UTF8
                Write-Host "  Inlined critical CSS: $($html.Name)" -ForegroundColor Gray
            }
        }
    }
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$websiteProjectPath = Join-Path $repoRoot $WebsiteProject
$wwwrootSource = [IO.Path]::Combine($repoRoot, "CodeGlyphX.Website", "wwwroot")
$publishRoot = Join-Path $repoRoot $OutputPath
$tempPublish = Join-Path $repoRoot "temp-playground"
$tempDocs = Join-Path $repoRoot "temp-docs"
$playgroundRoot = Join-Path $publishRoot "playground"
$docsRoot = Join-Path $publishRoot "docs"
$baseConstants = if ($Configuration -eq "Debug") { "DEBUG;TRACE" } else { "TRACE" }
$docsConstants = "$baseConstants;DOCS_BUILD"
$playgroundConstants = "$baseConstants;PLAYGROUND_BUILD"

if (-not (Test-Path $websiteProjectPath)) { throw "Missing website project at $websiteProjectPath" }
if (-not (Test-Path $wwwrootSource)) { throw "Missing wwwroot at $wwwrootSource" }

$projectFrameworks = Get-ProjectFrameworks -ProjectPath $websiteProjectPath
if ($projectFrameworks.Count -gt 0 -and -not ($projectFrameworks -contains $Framework)) {
    $requested = $Framework
    $Framework = $projectFrameworks[0]
    Write-Host "Requested framework '$requested' not found in project. Using '$Framework' instead." -ForegroundColor Yellow
}

if (Test-Path $publishRoot) {
    Remove-Item -Recurse -Force $publishRoot
}
New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null

Write-Host "Copying static assets..." -ForegroundColor Cyan
Copy-Item -Path (Join-Path $wwwrootSource "*") -Destination $publishRoot -Recurse -Force

New-Item -ItemType File -Path (Join-Path $publishRoot ".nojekyll") -Force | Out-Null

$staticPagesScript = Join-Path $PSScriptRoot "Generate-StaticPages.ps1"
Invoke-Step "Generating static pages..." @("pwsh",$staticPagesScript,"-OutputPath",$publishRoot)

if (Test-Path $tempPublish) {
    Remove-Item -Recurse -Force $tempPublish
}

Invoke-Step "Publishing docs (Blazor WASM)..." @(
    "dotnet","publish",$websiteProjectPath,
    "-c",$Configuration,
    "-f",$Framework,
    "-o",$tempDocs,
    "-p:BaseHref=/docs/",
    "-p:DefineConstants=`"$docsConstants`""
)

if (Test-Path $docsRoot) {
    Remove-Item -Recurse -Force $docsRoot
}
New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
Copy-Item -Path ([IO.Path]::Combine($tempDocs, "wwwroot", "*")) -Destination $docsRoot -Recurse -Force

$docsIndex = Join-Path $docsRoot "index.html"
$docs404 = Join-Path $docsRoot "404.html"
if (Test-Path $docsIndex) {
    Copy-Item -Path $docsIndex -Destination $docs404 -Force
}

Set-BaseHref -FilePath $docsIndex -BaseHref "/docs/"
Set-BaseHref -FilePath $docs404 -BaseHref "/docs/"
Copy-FingerprintedBlazorJs -FrameworkPath (Join-Path $docsRoot "_framework")
Update-BootIntegrity -FrameworkPath (Join-Path $docsRoot "_framework")
Add-BlazorScriptCacheBuster -HtmlPath $docsIndex
Add-BlazorScriptCacheBuster -HtmlPath $docs404

if (Test-Path $tempDocs) {
    Remove-Item -Recurse -Force $tempDocs
}

Invoke-Step "Publishing playground (Blazor WASM)..." @(
    "dotnet","publish",$websiteProjectPath,
    "-c",$Configuration,
    "-f",$Framework,
    "-o",$tempPublish,
    "-p:BaseHref=/playground/",
    "-p:DefineConstants=`"$playgroundConstants`""
)

New-Item -ItemType Directory -Path $playgroundRoot -Force | Out-Null
Copy-Item -Path ([IO.Path]::Combine($tempPublish, "wwwroot", "*")) -Destination $playgroundRoot -Recurse -Force

$playgroundIndex = Join-Path $playgroundRoot "index.html"
$playground404 = Join-Path $playgroundRoot "404.html"
if (Test-Path $playgroundIndex) {
    Copy-Item -Path $playgroundIndex -Destination $playground404 -Force
}

Set-BaseHref -FilePath $playgroundIndex -BaseHref "/playground/"
Set-BaseHref -FilePath $playground404 -BaseHref "/playground/"
Copy-FingerprintedBlazorJs -FrameworkPath (Join-Path $playgroundRoot "_framework")
Update-BootIntegrity -FrameworkPath (Join-Path $playgroundRoot "_framework")
Add-BlazorScriptCacheBuster -HtmlPath $playgroundIndex
Add-BlazorScriptCacheBuster -HtmlPath $playground404

if (Test-Path $tempPublish) {
    Remove-Item -Recurse -Force $tempPublish
}

# Minify CSS and JS assets if requested
if ($MinifyAssets) {
    Write-Host "Minifying CSS and JavaScript assets..." -ForegroundColor Cyan

    # Check if PSParseHTML module is available
    $psParseHtml = Get-Module -ListAvailable -Name PSParseHTML | Select-Object -First 1
    if (-not $psParseHtml) {
        Write-Warning "PSParseHTML module not found. Skipping minification."
        Write-Warning "Install with: Install-Module PSParseHTML -Force"
    } else {
        Import-Module PSParseHTML -Force

        # Minify CSS files
        $cssFiles = Get-ChildItem -Path $publishRoot -Filter "*.css" -Recurse -File
        foreach ($css in $cssFiles) {
            # Skip already minified files
            if ($css.Name -match '\.min\.css$') { continue }

            $originalSize = $css.Length
            try {
                Optimize-CSS -Path $css.FullName -OutputFile $css.FullName
                $newSize = (Get-Item $css.FullName).Length
                $saved = $originalSize - $newSize
                $percent = if ($originalSize -gt 0) { [math]::Round(($saved / $originalSize) * 100, 1) } else { 0 }
                Write-Host "  $($css.Name): $originalSize -> $newSize bytes (saved $saved bytes, $percent%)" -ForegroundColor Gray
            } catch {
                Write-Warning "  Failed to minify $($css.Name): $_"
            }
        }

        # Minify JS files (skip framework files)
        $jsFiles = Get-ChildItem -Path $publishRoot -Filter "*.js" -Recurse -File |
            Where-Object { $_.FullName -notmatch '_framework' -and $_.Name -notmatch '\.min\.js$' }
        foreach ($js in $jsFiles) {
            $originalSize = $js.Length
            try {
                Optimize-JavaScript -Path $js.FullName -OutputFile $js.FullName
                $newSize = (Get-Item $js.FullName).Length
                $saved = $originalSize - $newSize
                $percent = if ($originalSize -gt 0) { [math]::Round(($saved / $originalSize) * 100, 1) } else { 0 }
                Write-Host "  $($js.Name): $originalSize -> $newSize bytes (saved $saved bytes, $percent%)" -ForegroundColor Gray
            } catch {
                Write-Warning "  Failed to minify $($js.Name): $_"
            }
        }
    }
}

# Generate sitemap.xml
$sitemapScript = Join-Path $PSScriptRoot "Generate-Sitemap.ps1"
Invoke-Step "Generating sitemap.xml..." @("pwsh", $sitemapScript, "-SiteRoot", $publishRoot, "-SiteBase", "https://codeglyphx.com")

# Inline critical CSS for faster initial render
Write-Host "Inlining critical CSS..." -ForegroundColor Cyan
$criticalCssPath = Join-Path $wwwrootSource "css" "critical.css"
Invoke-InlineCriticalCss -SiteRoot $publishRoot -CriticalCssPath $criticalCssPath

Write-Host "Website output ready at $publishRoot" -ForegroundColor Green
