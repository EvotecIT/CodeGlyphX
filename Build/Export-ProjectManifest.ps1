[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string] $OutputPath = 'WebsiteArtifacts/project-manifest.json'
)

function Get-GitValue {
    param(
        [Parameter(Mandatory)][string] $RepoPath,
        [Parameter(Mandatory)][string[]] $Arguments
    )

    try {
        $Value = & git -C $RepoPath @Arguments 2>$null
        if ($LASTEXITCODE -ne 0) {
            return $null
        }
        return ($Value | Select-Object -First 1)
    } catch {
        return $null
    }
}

function Resolve-Version {
    param(
        [Parameter(Mandatory)][string] $RepoPath
    )

    $Tag = Get-GitValue -RepoPath $RepoPath -Arguments @('describe', '--tags', '--abbrev=0')
    if ($Tag) {
        $Normalized = [string]$Tag
        $Normalized = $Normalized -replace '^CodeGlyphX-v', ''
        $Normalized = $Normalized -replace '^v', ''
        if ($Normalized) {
            return $Normalized
        }
    }

    $ProjectPath = Join-Path $RepoPath 'CodeGlyphX/CodeGlyphX.csproj'
    if (Test-Path -LiteralPath $ProjectPath) {
        $VersionPrefix = Select-String -Path $ProjectPath -Pattern '<VersionPrefix>([^<]+)</VersionPrefix>' -AllMatches | Select-Object -First 1
        if ($VersionPrefix -and $VersionPrefix.Matches.Count -gt 0) {
            return $VersionPrefix.Matches[0].Groups[1].Value
        }
    }

    return '1.0.0'
}

$RepoRootResolved = [System.IO.Path]::GetFullPath($RepoRoot)
$OutputPathResolved = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    [System.IO.Path]::GetFullPath($OutputPath)
} else {
    [System.IO.Path]::GetFullPath((Join-Path $RepoRootResolved $OutputPath))
}
$OutputDirectory = Split-Path -Path $OutputPathResolved -Parent
if (-not (Test-Path -LiteralPath $OutputDirectory)) {
    $null = New-Item -Path $OutputDirectory -ItemType Directory -Force
}

$Version = Resolve-Version -RepoPath $RepoRootResolved
$Commit = Get-GitValue -RepoPath $RepoRootResolved -Arguments @('rev-parse', '--short', 'HEAD')
$GeneratedAt = Get-GitValue -RepoPath $RepoRootResolved -Arguments @('show', '-s', '--format=%cI', 'HEAD')
if (-not $GeneratedAt) {
    $GeneratedAt = (Get-Date).ToUniversalTime().ToString('o')
}

$Manifest = [ordered]@{
    slug        = 'codeglyphx'
    name        = 'CodeGlyphX'
    mode        = 'hub-full'
    version     = $Version
    generatedAt = [string]$GeneratedAt
    commit      = [string]$Commit
    description = 'No-deps QR & barcode toolkit for .NET.'
    surfaces    = [ordered]@{
        docs          = $true
        apiDotNet     = $true
        apiPowerShell = $false
        changelog     = $true
        releases      = $true
    }
    links       = [ordered]@{
        website       = 'https://codeglyphx.com'
        docs          = 'https://codeglyphx.com/docs/'
        apiDotNet     = 'https://codeglyphx.com/api/'
        changelog     = 'https://codeglyphx.com/changelog/'
        releases      = 'https://github.com/EvotecIT/CodeGlyphX/releases'
        source        = 'https://github.com/EvotecIT/CodeGlyphX'
    }
}

if ($PSCmdlet.ShouldProcess($OutputPathResolved, 'Write project-manifest.json')) {
    $Manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $OutputPathResolved -Encoding utf8
}

[PSCustomObject]@{
    outputPath = $OutputPathResolved
    version    = $Version
    commit     = $Commit
}
