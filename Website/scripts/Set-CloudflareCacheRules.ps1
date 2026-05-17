param(
    [string] $ZoneId = $env:CLOUDFLARE_ZONE_ID,
    [string] $ApiToken = $env:CLOUDFLARE_API_TOKEN,
    [string] $HostName = 'codeglyphx.com'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ZoneId)) {
    throw 'CLOUDFLARE_ZONE_ID is required to configure cache rules.'
}

if ([string]::IsNullOrWhiteSpace($ApiToken)) {
    throw 'CLOUDFLARE_API_TOKEN is required to configure cache rules.'
}

$phase = 'http_request_cache_settings'
$endpoint = "https://api.cloudflare.com/client/v4/zones/$ZoneId/rulesets/phases/$phase/entrypoint"
$headers = @{
    Authorization = "Bearer $ApiToken"
}

function Invoke-CloudflareJson {
    param(
        [Parameter(Mandatory)]
        [ValidateSet('GET', 'PUT')]
        [string] $Method,

        [Parameter(Mandatory)]
        [string] $Uri,

        [object] $Body
    )

    $parameters = @{
        Method      = $Method
        Uri         = $Uri
        Headers     = $headers
        ContentType = 'application/json'
    }

    if ($null -ne $Body) {
        $parameters.Body = ($Body | ConvertTo-Json -Depth 32)
    }

    try {
        return Invoke-RestMethod @parameters
    } catch {
        $response = $_.Exception.Response
        if ($response -and [int]$response.StatusCode -eq 404) {
            return $null
        }

        throw
    }
}

function New-CacheRule {
    param(
        [Parameter(Mandatory)]
        [string] $Description,

        [Parameter(Mandatory)]
        [string] $Expression
    )

    [ordered]@{
        description       = $Description
        expression        = $Expression
        action            = 'set_cache_settings'
        action_parameters = [ordered]@{
            cache                 = $true
            edge_ttl              = [ordered]@{
                mode = 'respect_origin'
            }
            browser_ttl           = [ordered]@{
                mode = 'respect_origin'
            }
            respect_strong_etags  = $true
        }
        enabled           = $true
    }
}

$hostFilter = "(http.host eq `"$HostName`" and http.request.method eq `"GET`" and "

$desiredRules = @(
    (New-CacheRule `
        -Description 'PowerForge CodeGlyphX: static assets' `
        -Expression ($hostFilter + '(http.request.uri.path wildcard "/css/*" or http.request.uri.path wildcard "/js/*" or http.request.uri.path wildcard "/assets/*" or http.request.uri.path wildcard "/fonts/*" or http.request.uri.path wildcard "/images/*" or http.request.uri.path wildcard "/img/*" or http.request.uri.path wildcard "/*.css" or http.request.uri.path wildcard "/*.js" or http.request.uri.path wildcard "/*.mjs" or http.request.uri.path wildcard "/*.png" or http.request.uri.path wildcard "/*.jpg" or http.request.uri.path wildcard "/*.jpeg" or http.request.uri.path wildcard "/*.webp" or http.request.uri.path wildcard "/*.svg" or http.request.uri.path wildcard "/*.ico" or http.request.uri.path wildcard "/*.woff" or http.request.uri.path wildcard "/*.woff2"))')),
    (New-CacheRule `
        -Description 'PowerForge CodeGlyphX: data files' `
        -Expression ($hostFilter + '(http.request.uri.path wildcard "/data/*" or http.request.uri.path eq "/sitemap.xml" or http.request.uri.path eq "/llms.txt" or http.request.uri.path eq "/llms-full.txt" or http.request.uri.path eq "/llms.json"))')),
    (New-CacheRule `
        -Description 'PowerForge CodeGlyphX: HTML docs and API' `
        -Expression ($hostFilter + '(http.request.uri.path eq "/" or http.request.uri.path wildcard "/docs/*" or http.request.uri.path wildcard "/api/*" or http.request.uri.path wildcard "/blog/*" or http.request.uri.path wildcard "/showcase/*" or http.request.uri.path wildcard "/playground/*" or http.request.uri.path wildcard "/pricing/*" or http.request.uri.path wildcard "/benchmarks/*" or http.request.uri.path wildcard "/faq/*" or http.request.uri.path eq "/search/" or http.request.uri.path wildcard "/search/*" or http.request.uri.path wildcard "*.html"))'))
)

$current = Invoke-CloudflareJson -Method GET -Uri $endpoint
$existingRules = @()

if ($null -ne $current -and $current.result -and $current.result.rules) {
    $existingRules = @($current.result.rules) | Where-Object {
        $description = [string]($_.description)
        -not $description.StartsWith('PowerForge CodeGlyphX:', [System.StringComparison]::Ordinal)
    }
}

$payload = [ordered]@{
    name  = 'PowerForge cache rules'
    kind  = 'zone'
    phase = $phase
    rules = @($desiredRules + $existingRules)
}

$updated = Invoke-CloudflareJson -Method PUT -Uri $endpoint -Body $payload

if (-not $updated.success) {
    $errors = @($updated.errors | ForEach-Object { $_.message }) -join '; '
    if ([string]::IsNullOrWhiteSpace($errors)) {
        $errors = 'Cloudflare API did not return success.'
    }

    throw $errors
}

Start-Sleep -Seconds 10
Write-Host "Configured $($desiredRules.Count) PowerForge cache rule(s) for $HostName."
