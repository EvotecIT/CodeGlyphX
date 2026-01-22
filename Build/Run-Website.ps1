param(
    [string]$Configuration = "Debug",
    [string]$Framework = "net10.0",
    [string]$BaseUrl = "http://localhost:5000/api/",
    [int]$Port = 5001,
    [switch]$SkipApiDocs,
    [switch]$SkipLlms,
    [switch]$Watch,
    [switch]$Spa,
    [string]$OutputPath = "site"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$buildScript = Join-Path $PSScriptRoot "Build-Website.ps1"
$publishScript = Join-Path $PSScriptRoot "Publish-WebsitePages.ps1"
$websiteProject = Join-Path (Join-Path $repoRoot "CodeGlyphX.Website") "CodeGlyphX.Website.csproj"

$BaseUrl = "http://localhost:$Port/api/"
$env:ASPNETCORE_URLS = "http://localhost:$Port"

function Test-PortAvailable {
    param([int]$PortNumber)
    $listener = $null
    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $PortNumber)
        $listener.Start()
        return $true
    } catch {
        return $false
    } finally {
        if ($listener) { $listener.Stop() }
    }
}

function Stop-ProcessOnPort {
    param([int]$PortNumber)

    # Get process IDs using the port
    $connections = netstat -ano | Select-String ":$PortNumber\s" | ForEach-Object {
        if ($_ -match '\s(\d+)$') {
            [int]$matches[1]
        }
    } | Where-Object { $_ -gt 0 } | Select-Object -Unique

    foreach ($pid in $connections) {
        try {
            $proc = Get-Process -Id $pid -ErrorAction SilentlyContinue
            if ($proc) {
                Write-Host "Stopping process $($proc.ProcessName) (PID: $pid) using port $PortNumber..." -ForegroundColor Yellow
                Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
                Start-Sleep -Milliseconds 500
            }
        } catch {
            # Process may have already exited
        }
    }
}

function Get-FreePort {
    param([int]$StartPort, [int]$Attempts = 25)
    for ($offset = 0; $offset -lt $Attempts; $offset++) {
        $candidate = $StartPort + $offset
        if (Test-PortAvailable -PortNumber $candidate) {
            return $candidate
        }
    }
    throw "No free port found starting at $StartPort."
}

if ($Spa) {
    # Kill any existing process using the port
    if (-not (Test-PortAvailable -PortNumber $Port)) {
        Write-Host "Port $Port is in use. Attempting to free it..." -ForegroundColor Yellow
        Stop-ProcessOnPort -PortNumber $Port
        Start-Sleep -Seconds 1
        if (-not (Test-PortAvailable -PortNumber $Port)) {
            throw "Failed to free port $Port. Please manually stop the process using it."
        }
        Write-Host "Port $Port is now available." -ForegroundColor Green
    }

    # Restore NuGet packages to ensure all dependencies are available
    Write-Host "Restoring packages..." -ForegroundColor Cyan
    & dotnet restore $websiteProject --verbosity quiet

    & $buildScript -Configuration $Configuration -Framework $Framework -BaseUrl $BaseUrl -SkipApiDocs:$SkipApiDocs -SkipLlms:$SkipLlms

    Write-Host "Website dev routes:" -ForegroundColor Cyan
    Write-Host "  Home:       http://localhost:$Port/" -ForegroundColor DarkGray
    Write-Host "  Playground: http://localhost:$Port/playground" -ForegroundColor DarkGray
    Write-Host "  Docs:       http://localhost:$Port/docs" -ForegroundColor DarkGray
    Write-Host "  Showcase:   http://localhost:$Port/showcase" -ForegroundColor DarkGray
    Write-Host "  FAQ:        http://localhost:$Port/faq" -ForegroundColor DarkGray

    if ($Watch) {
        Write-Host "Starting website (watch mode)..." -ForegroundColor Cyan
        & dotnet watch --project $websiteProject run -c $Configuration -f $Framework
    } else {
        Write-Host "Starting website..." -ForegroundColor Cyan
        & dotnet run --project $websiteProject -c $Configuration -f $Framework
    }
    return
}

Write-Host "Building production-like website..." -ForegroundColor Cyan
& $publishScript -Configuration $Configuration -Framework $Framework -OutputPath $OutputPath

$siteRoot = Join-Path $repoRoot $OutputPath
if (-not (Test-Path $siteRoot)) {
    throw "Static site output not found at $siteRoot"
}

Write-Host "Static site root: $siteRoot" -ForegroundColor DarkGray

# Kill any existing process using the port
if (-not (Test-PortAvailable -PortNumber $Port)) {
    Write-Host "Port $Port is in use. Attempting to free it..." -ForegroundColor Yellow
    Stop-ProcessOnPort -PortNumber $Port
    Start-Sleep -Seconds 1
}

$serverPort = Get-FreePort -StartPort $Port
Write-Host "Serving on http://localhost:$serverPort/" -ForegroundColor Cyan

function Get-ContentType {
    param([string]$Path)
    switch ([IO.Path]::GetExtension($Path).ToLowerInvariant()) {
        ".html" { "text/html; charset=utf-8" }
        ".css" { "text/css; charset=utf-8" }
        ".js" { "text/javascript; charset=utf-8" }
        ".json" { "application/json; charset=utf-8" }
        ".xml" { "application/xml; charset=utf-8" }
        ".svg" { "image/svg+xml" }
        ".png" { "image/png" }
        ".jpg" { "image/jpeg" }
        ".jpeg" { "image/jpeg" }
        ".gif" { "image/gif" }
        ".ico" { "image/x-icon" }
        ".woff2" { "font/woff2" }
        ".wasm" { "application/wasm" }
        ".txt" { "text/plain; charset=utf-8" }
        default { "application/octet-stream" }
    }
}

$listener = $null
try {
    $listener = New-Object System.Net.HttpListener
    $listener.Prefixes.Add("http://localhost:$serverPort/")
    $listener.Start()
} catch {
    $listener = $null
}

if ($null -eq $listener -or -not $listener.IsListening) {
    $python = Get-Command python -ErrorAction SilentlyContinue
    if (-not $python) { $python = Get-Command py -ErrorAction SilentlyContinue }
    if (-not $python) {
        throw "Failed to start HttpListener and Python not found for fallback server."
    }
    Write-Host "HttpListener unavailable; falling back to Python server." -ForegroundColor Yellow
    & $python.Source "-m" "http.server" $serverPort "--directory" $siteRoot
    return
}

$watcher = $null
$buildPending = $false
$lastChange = Get-Date
$buildInProgress = $false
$eventPrefix = $null

if ($Watch) {
    Get-EventSubscriber | Where-Object SourceIdentifier -like "WebsiteChanged-*" | ForEach-Object { Unregister-Event -SubscriptionId $_.SubscriptionId }
    $watcher = New-Object IO.FileSystemWatcher (Join-Path $repoRoot "CodeGlyphX.Website"), "*.*"
    $watcher.IncludeSubdirectories = $true
    $watcher.NotifyFilter = [IO.NotifyFilters]::LastWrite -bor [IO.NotifyFilters]::FileName -bor [IO.NotifyFilters]::DirectoryName
    $watcher.EnableRaisingEvents = $true

    $eventPrefix = "WebsiteChanged-" + [Guid]::NewGuid().ToString("N")
    Register-ObjectEvent $watcher Changed -SourceIdentifier ($eventPrefix + "-Changed") | Out-Null
    Register-ObjectEvent $watcher Created -SourceIdentifier ($eventPrefix + "-Created") | Out-Null
    Register-ObjectEvent $watcher Deleted -SourceIdentifier ($eventPrefix + "-Deleted") | Out-Null
    Register-ObjectEvent $watcher Renamed -SourceIdentifier ($eventPrefix + "-Renamed") | Out-Null
}

Write-Host "Press Ctrl+C to stop." -ForegroundColor DarkGray
try {
    while ($listener.IsListening) {
        if ($Watch) {
            $evt = Wait-Event -Timeout 0.2
            if ($evt -and $evt.SourceIdentifier -like ($eventPrefix + "*")) {
                $lastChange = Get-Date
                $buildPending = $true
                Remove-Event -EventIdentifier $evt.EventIdentifier
            } elseif ($evt) {
                Remove-Event -EventIdentifier $evt.EventIdentifier
            }

            if ($buildPending -and -not $buildInProgress) {
                if (((Get-Date) - $lastChange).TotalMilliseconds -gt 500) {
                    $buildPending = $false
                    $buildInProgress = $true
                    Write-Host "Rebuilding static site..." -ForegroundColor Cyan
                    & $publishScript -Configuration $Configuration -Framework $Framework -OutputPath $OutputPath
                    $buildInProgress = $false
                }
            }
        }

        if ($listener.IsListening) {
            $context = $listener.GetContext()
            $requestPath = [Uri]::UnescapeDataString($context.Request.Url.AbsolutePath.TrimStart('/'))
            $targetPath = if ([string]::IsNullOrWhiteSpace($requestPath)) { "index.html" } else { $requestPath }
            $fullPath = Join-Path $siteRoot $targetPath

            if (Test-Path $fullPath -PathType Container) {
                $fullPath = Join-Path $fullPath "index.html"
            }

            if (-not (Test-Path $fullPath)) {
                $fallback = Join-Path $siteRoot "404.html"
                if (Test-Path $fallback) {
                    $fullPath = $fallback
                    $context.Response.StatusCode = 404
                } else {
                    $context.Response.StatusCode = 404
                    $context.Response.Close()
                    continue
                }
            }

            $bytes = [IO.File]::ReadAllBytes($fullPath)
            $context.Response.ContentType = Get-ContentType -Path $fullPath
            $context.Response.ContentLength64 = $bytes.Length
            try {
                $context.Response.OutputStream.Write($bytes, 0, $bytes.Length)
            } catch {
                # Client disconnected mid-response; ignore to keep the dev server running.
            } finally {
                try {
                    $context.Response.OutputStream.Close()
                } catch {
                    # Ignore shutdown errors from aborted connections.
                }
            }
        }
    }
} finally {
    if ($listener) {
        $listener.Stop()
        $listener.Close()
    }
    if ($watcher) {
        $watcher.EnableRaisingEvents = $false
        $watcher.Dispose()
    }
    if ($eventPrefix) {
        Get-EventSubscriber | Where-Object SourceIdentifier -like ($eventPrefix + "*") | ForEach-Object { Unregister-Event -SubscriptionId $_.SubscriptionId }
    }
}
