param(
    [string]$Manifest = "CodeGlyphX.Tests/Fixtures/ImageSamples/manifest.json",
    [string]$Destination = "CodeGlyphX.Tests/Fixtures/ImageSamples",
    [switch]$Force,
    [switch]$NoUpdateManifest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-EntryProperty {
    param(
        [Parameter(Mandatory = $true)]$Entry,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($Entry.PSObject.Properties.Name -contains $Name) {
        return $Entry.$Name
    }
    return $null
}

if (-not (Test-Path $Manifest)) {
    throw "Manifest not found: $Manifest"
}

$manifestText = Get-Content -Path $Manifest -Raw -Encoding utf8
$manifestData = $manifestText | ConvertFrom-Json
$hasEntries = $manifestData -and ($manifestData.PSObject.Properties.Name -contains 'entries')
$entries = @()
if ($hasEntries) {
    $entries = @($manifestData.entries)
} elseif ($manifestData -is [System.Collections.IEnumerable] -and -not ($manifestData -is [string])) {
    $entries = @($manifestData)
}
if ($entries.Count -eq 0) {
    throw "No entries in manifest: $Manifest"
}

New-Item -ItemType Directory -Path $Destination -Force | Out-Null

foreach ($entry in $entries) {
    $downloadUrl = Get-EntryProperty -Entry $entry -Name "downloadUrl"
    $archiveUrl = Get-EntryProperty -Entry $entry -Name "archiveUrl"
    $archivePath = Get-EntryProperty -Entry $entry -Name "archivePath"
    if (-not $downloadUrl -and -not $archiveUrl) {
        throw "Missing downloadUrl/archiveUrl in manifest entry: $($entry.id)"
    }

    $fileName = Get-EntryProperty -Entry $entry -Name "fileName"
    if (-not $fileName) {
        if ($downloadUrl) {
            $fileName = [IO.Path]::GetFileName($downloadUrl)
        } elseif ($archivePath) {
            $fileName = [IO.Path]::GetFileName($archivePath)
        }
    }
    if (-not $fileName) {
        throw "Missing fileName in manifest entry: $($entry.id)"
    }

    $outPath = Join-Path $Destination $fileName
    $entrySha = Get-EntryProperty -Entry $entry -Name "sha256"

    if (-not $Force -and (Test-Path $outPath)) {
        if ($entrySha) {
            $existingHash = (Get-FileHash -Algorithm SHA256 -Path $outPath).Hash.ToLowerInvariant()
            if ($existingHash -ne $entrySha.ToLowerInvariant()) {
                throw "Hash mismatch for existing file: $outPath"
            }
        }
    } else {
        if ($archiveUrl) {
            if (-not $archivePath) {
                throw "Missing archivePath for manifest entry: $($entry.id)"
            }
            $cacheDir = Join-Path ([IO.Path]::GetTempPath()) "codeglyphx-samples"
            New-Item -ItemType Directory -Path $cacheDir -Force | Out-Null
            $archiveName = [IO.Path]::GetFileName($archiveUrl)
            if (-not $archiveName) {
                throw "Invalid archiveUrl in manifest entry: $($entry.id)"
            }
            $archiveFile = Join-Path $cacheDir $archiveName
            if ($Force -or -not (Test-Path $archiveFile)) {
                Invoke-WebRequest -Uri $archiveUrl -OutFile $archiveFile -UseBasicParsing
            }

            $extractDir = Join-Path $cacheDir ([Guid]::NewGuid().ToString("N"))
            New-Item -ItemType Directory -Path $extractDir -Force | Out-Null
            try {
                & tar -xf $archiveFile -C $extractDir $archivePath | Out-Null
            } catch {
                throw "Failed to extract archive entry: $archivePath from $archiveUrl"
            }
            $localPath = Join-Path $extractDir ($archivePath -replace '/', '\')
            if (-not (Test-Path $localPath)) {
                throw "Archive entry not found after extraction: $archivePath"
            }
            Copy-Item -Path $localPath -Destination $outPath -Force
            Remove-Item -Path $extractDir -Recurse -Force
        } else {
            Invoke-WebRequest -Uri $downloadUrl -OutFile $outPath -UseBasicParsing
        }
    }

    $hash = (Get-FileHash -Algorithm SHA256 -Path $outPath).Hash.ToLowerInvariant()
    if ($entrySha -and $hash -ne $entrySha.ToLowerInvariant()) {
        throw "Hash mismatch for downloaded file: $outPath"
    }
    if (-not $entrySha -and -not $NoUpdateManifest) {
        $entry | Add-Member -NotePropertyName sha256 -NotePropertyValue $hash
    }
}

if (-not $NoUpdateManifest) {
    if ($hasEntries) {
        $manifestData | ConvertTo-Json -Depth 6 | Set-Content -Path $Manifest -Encoding utf8
    } else {
        $entries | ConvertTo-Json -Depth 6 | Set-Content -Path $Manifest -Encoding utf8
    }
}

Write-Host "Image samples synced to $Destination"
