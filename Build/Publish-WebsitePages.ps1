param(
    [string]$Configuration = "Release",
    [string]$Framework = "net10.0",
    [string]$WebsiteProject = "CodeGlyphX.Website/CodeGlyphX.Website.csproj",
    [string]$OutputPath = "site"
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

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$websiteProjectPath = Join-Path $repoRoot $WebsiteProject
$wwwrootSource = Join-Path $repoRoot "CodeGlyphX.Website" "wwwroot"
$publishRoot = Join-Path $repoRoot $OutputPath
$tempPublish = Join-Path $repoRoot "temp-playground"
$tempDocs = Join-Path $repoRoot "temp-docs"
$playgroundRoot = Join-Path $publishRoot "playground"
$docsRoot = Join-Path $publishRoot "docs"
$baseConstants = if ($Configuration -eq "Debug") { "DEBUG%3BTRACE" } else { "TRACE" }
$docsConstants = "$baseConstants%3BDOCS_BUILD"
$playgroundConstants = "$baseConstants%3BPLAYGROUND_BUILD"

if (-not (Test-Path $websiteProjectPath)) { throw "Missing website project at $websiteProjectPath" }
if (-not (Test-Path $wwwrootSource)) { throw "Missing wwwroot at $wwwrootSource" }

if (Test-Path $publishRoot) {
    Remove-Item -Recurse -Force $publishRoot
}
New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null

Write-Host "Copying static assets..." -ForegroundColor Cyan
Copy-Item -Path (Join-Path $wwwrootSource "*") -Destination $publishRoot -Recurse -Force

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
    "-p:DefineConstants=$docsConstants"
)

if (Test-Path $docsRoot) {
    Remove-Item -Recurse -Force $docsRoot
}
New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
Copy-Item -Path (Join-Path $tempDocs "wwwroot" "*") -Destination $docsRoot -Recurse -Force

$docsCompressed = Get-ChildItem -Path $docsRoot -Recurse -Include *.br, *.gz -File -ErrorAction SilentlyContinue
if ($docsCompressed) {
    $docsCompressed | Remove-Item -Force
}

$docsIndex = Join-Path $docsRoot "index.html"
$docs404 = Join-Path $docsRoot "404.html"
if (Test-Path $docsIndex) {
    Copy-Item -Path $docsIndex -Destination $docs404 -Force
}

Set-BaseHref -FilePath $docsIndex -BaseHref "/docs/"
Set-BaseHref -FilePath $docs404 -BaseHref "/docs/"
Update-BootIntegrity -FrameworkPath (Join-Path $docsRoot "_framework")

if (Test-Path $tempDocs) {
    Remove-Item -Recurse -Force $tempDocs
}

Invoke-Step "Publishing playground (Blazor WASM)..." @(
    "dotnet","publish",$websiteProjectPath,
    "-c",$Configuration,
    "-f",$Framework,
    "-o",$tempPublish,
    "-p:BaseHref=/playground/",
    "-p:DefineConstants=$playgroundConstants"
)

New-Item -ItemType Directory -Path $playgroundRoot -Force | Out-Null
Copy-Item -Path (Join-Path $tempPublish "wwwroot" "*") -Destination $playgroundRoot -Recurse -Force

$compressed = Get-ChildItem -Path $playgroundRoot -Recurse -Include *.br, *.gz -File -ErrorAction SilentlyContinue
if ($compressed) {
    $compressed | Remove-Item -Force
}

$playgroundIndex = Join-Path $playgroundRoot "index.html"
$playground404 = Join-Path $playgroundRoot "404.html"
if (Test-Path $playgroundIndex) {
    Copy-Item -Path $playgroundIndex -Destination $playground404 -Force
}

Set-BaseHref -FilePath $playgroundIndex -BaseHref "/playground/"
Set-BaseHref -FilePath $playground404 -BaseHref "/playground/"
Update-BootIntegrity -FrameworkPath (Join-Path $playgroundRoot "_framework")

if (Test-Path $tempPublish) {
    Remove-Item -Recurse -Force $tempPublish
}
Write-Host "Website output ready at $publishRoot" -ForegroundColor Green
