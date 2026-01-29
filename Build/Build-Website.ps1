param(
    [string]$Configuration = "Release",
    [string]$Framework = "net10.0",
    [string]$BaseUrl = "https://codeglyphx.com/api/",
    [string]$SiteTitle = "CodeGlyphX API Reference",
    [string]$PowerForgeCliProject,
    [string]$WebsiteProject = "CodeGlyphX.Website/CodeGlyphX.Website.csproj",
    [string]$OutputPath = "site",
    [switch]$SkipApiDocs,
    [switch]$SkipLlms,
    [switch]$SkipStyleBoard,
    [switch]$Publish
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

if ($IsLinux -or $IsMacOS) {
    if ($env:NUGET_FALLBACK_PACKAGES -and $env:NUGET_FALLBACK_PACKAGES -match "C:\\") {
        $env:NUGET_FALLBACK_PACKAGES = ""
    }
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
if (-not $PowerForgeCliProject) {
    $PowerForgeCliProject = [IO.Path]::Combine($repoRoot, "..", "PSPublishModule", "PowerForge.Cli", "PowerForge.Cli.csproj")
}

$codeGlyphProject = [IO.Path]::Combine($repoRoot, "CodeGlyphX", "CodeGlyphX.csproj")
$websiteProjectPath = Join-Path $repoRoot $WebsiteProject
$assemblyPath = [IO.Path]::Combine($repoRoot, "CodeGlyphX", "bin", $Configuration, $Framework, "CodeGlyphX.dll")
$xmlPath = [IO.Path]::Combine($repoRoot, "CodeGlyphX", "bin", $Configuration, $Framework, "CodeGlyphX.xml")
$apiOutput = [IO.Path]::Combine($repoRoot, "CodeGlyphX.Website", "wwwroot", "api")
$apiCssPath = [IO.Path]::Combine($repoRoot, "CodeGlyphX.Website", "wwwroot", "css", "api-docs.css")
$apiCssUrl = "/css/api-docs.css"
$apiHeader = [IO.Path]::Combine($repoRoot, "CodeGlyphX.Website", "wwwroot", "api-fragments", "header.html")
$apiFooter = [IO.Path]::Combine($repoRoot, "CodeGlyphX.Website", "wwwroot", "api-fragments", "footer.html")

if (-not (Test-Path $codeGlyphProject)) { throw "Missing CodeGlyphX.csproj at $codeGlyphProject" }
if (-not (Test-Path $websiteProjectPath)) { throw "Missing website project at $websiteProjectPath" }
if (-not (Test-Path $PowerForgeCliProject -PathType Leaf)) { throw "Missing PowerForge.Cli project at $PowerForgeCliProject" }
if (-not (Test-Path $apiCssPath)) { throw "Missing API docs CSS at $apiCssPath" }
if (-not (Test-Path $apiHeader)) { throw "Missing API docs header at $apiHeader" }
if (-not (Test-Path $apiFooter)) { throw "Missing API docs footer at $apiFooter" }

$navScript = Join-Path $PSScriptRoot "Update-NavFragments.ps1"
Invoke-Step "Updating navigation fragments..." @("pwsh",$navScript)

Invoke-Step "Building CodeGlyphX..." @("dotnet","build",$codeGlyphProject,"-c",$Configuration,"-f",$Framework)

if (-not $SkipStyleBoard) {
    $styleBoardScript = Join-Path $PSScriptRoot "Update-StyleBoard.ps1"
    Invoke-Step "Updating style board assets..." @("pwsh",$styleBoardScript,"-Configuration",$Configuration,"-Framework","net8.0")
}

if (-not $SkipApiDocs) {
    Invoke-Step "Generating API docs..." @(
        "dotnet","run","-c",$Configuration,"-f",$Framework,"--project",$PowerForgeCliProject,"--","apidocs",
        "--assembly",$assemblyPath,
        "--xml",$xmlPath,
        "--out",$apiOutput,
        "--format","hybrid",
        "--title",$SiteTitle,
        "--base-url",$BaseUrl,
        "--css",$apiCssUrl,
        "--header-html",$apiHeader,
        "--footer-html",$apiFooter
    )
}

if (-not $SkipLlms) {
    $llmsScript = Join-Path $PSScriptRoot "Generate-Llms.ps1"
    Invoke-Step "Generating llms.txt..." @("pwsh",$llmsScript,"-ApiBase","/api")
}

$siteBase = $BaseUrl.TrimEnd('/')
if ($siteBase.EndsWith("/api", [System.StringComparison]::OrdinalIgnoreCase)) {
    $siteBase = $siteBase.Substring(0, $siteBase.Length - 4)
}
$siteBase = $siteBase.TrimEnd('/')
$sitemapScript = Join-Path $PSScriptRoot "Generate-Sitemap.ps1"
Invoke-Step "Generating sitemap.xml..." @("pwsh",$sitemapScript,"-SiteBase",$siteBase)

if ($Publish) {
    # Publish into a separate output folder to avoid overwriting Blazor host files in wwwroot.
    $publishScript = Join-Path $PSScriptRoot "Publish-WebsitePages.ps1"
    Invoke-Step "Publishing static website..." @("pwsh",$publishScript,"-Configuration",$Configuration,"-Framework",$Framework,"-OutputPath",$OutputPath)
    return
} else {
    # Development build - no static pages, keep Blazor for all routes
    # Clean up any existing static HTML files that would conflict with SPA routing
    $wwwrootPath = [IO.Path]::Combine($repoRoot, "CodeGlyphX.Website", "wwwroot")
    $staticFolders = @("docs", "playground", "showcase", "faq")
    foreach ($folder in $staticFolders) {
        $indexFile = Join-Path $wwwrootPath "$folder\index.html"
        if (Test-Path $indexFile) {
            Write-Host "  Removing static $folder/index.html for SPA mode..." -ForegroundColor DarkGray
            Remove-Item $indexFile -Force
        }
    }

    # Restore Blazor index.html if it was overwritten by static page generation
    $blazorIndexPath = Join-Path $wwwrootPath "index.html"
    $hasBlazorScript = $false
    if (Test-Path $blazorIndexPath) {
        $content = Get-Content $blazorIndexPath -Raw
        $hasBlazorScript = $content -match 'blazor\.webassembly\.js'
    }
    if (-not $hasBlazorScript) {
        Write-Host "  Restoring Blazor index.html for SPA mode..." -ForegroundColor DarkGray
        Push-Location $repoRoot
        try {
            & git checkout HEAD -- "CodeGlyphX.Website/wwwroot/index.html" 2>$null
        } finally {
            Pop-Location
        }
    }

    Invoke-Step "Building website..." @("dotnet","build",$websiteProjectPath,"-c",$Configuration,"-f",$Framework)
}
