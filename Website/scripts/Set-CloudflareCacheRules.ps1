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

function Get-CloudflareLogUri {
    param(
        [Parameter(Mandatory)]
        [string] $Uri
    )

    if ([string]::IsNullOrWhiteSpace($ZoneId)) {
        return $Uri
    }

    return $Uri.Replace($ZoneId, '<zone>')
}

function ConvertTo-OneLine {
    param(
        [Parameter(Mandatory)]
        [string] $Value
    )

    return ($Value -replace '\s+', ' ').Trim()
}

function Get-CloudflareStatusDetail {
    param(
        [object] $Response
    )

    $statusCode = $null
    if ($response) {
        try {
            $statusCode = [int]$response.StatusCode
        } catch {
            Write-Verbose "Cloudflare error response did not expose a numeric status code: $($_.Exception.Message)"
        }
    }

    if ($statusCode -gt 0) {
        return "HTTP $statusCode"
    }

    return $null
}

function Get-CloudflareErrorBody {
    param(
        [Parameter(Mandatory)]
        [System.Management.Automation.ErrorRecord] $ErrorRecord
    )

    if ($ErrorRecord.ErrorDetails -and -not [string]::IsNullOrWhiteSpace($ErrorRecord.ErrorDetails.Message)) {
        return $ErrorRecord.ErrorDetails.Message
    }

    $response = $ErrorRecord.Exception.Response
    if (-not $response) {
        return $null
    }

    try {
        $stream = $response.GetResponseStream()
        if (-not $stream) {
            return $null
        }

        $reader = [System.IO.StreamReader]::new($stream)
        try {
            return $reader.ReadToEnd()
        } finally {
            $reader.Dispose()
        }
    } catch {
        Write-Verbose "Cloudflare error response body could not be read: $($_.Exception.Message)"
        return $null
    }
}

function Format-CloudflareApiMessages {
    param(
        [object[]] $Messages
    )

    return @($Messages | Where-Object { $_ } | ForEach-Object {
        $codeProperty = $_.PSObject.Properties['code']
        $messageProperty = $_.PSObject.Properties['message']
        $code = if ($codeProperty -and $codeProperty.Value) { "[$($codeProperty.Value)] " } else { '' }
        $message = if ($messageProperty) { [string]$messageProperty.Value } else { ConvertTo-OneLine -Value ([string]$_) }
        "$code$message"
    })
}

function Get-CloudflareBodyDetail {
    param(
        [string] $Body
    )

    if ([string]::IsNullOrWhiteSpace($Body)) {
        return $null
    }

    try {
        $json = $Body | ConvertFrom-Json -ErrorAction Stop
        $messages = @()
        $messages += Format-CloudflareApiMessages -Messages @($json.errors)
        $messages += Format-CloudflareApiMessages -Messages @($json.messages)

        if ($messages.Count -gt 0) {
            return ($messages -join '; ')
        }
    } catch {
        Write-Verbose "Cloudflare error response body was not JSON: $($_.Exception.Message)"
    }

    return ConvertTo-OneLine -Value $Body
}

function Get-CloudflareErrorDetail {
    param(
        [Parameter(Mandatory)]
        [System.Management.Automation.ErrorRecord] $ErrorRecord
    )

    $parts = [System.Collections.Generic.List[string]]::new()
    $statusDetail = Get-CloudflareStatusDetail -Response $ErrorRecord.Exception.Response
    $body = Get-CloudflareErrorBody -ErrorRecord $ErrorRecord
    $bodyDetail = Get-CloudflareBodyDetail -Body $body

    if (-not [string]::IsNullOrWhiteSpace($statusDetail)) {
        $parts.Add($statusDetail)
    }

    if (-not [string]::IsNullOrWhiteSpace($bodyDetail)) {
        $parts.Add($bodyDetail)
    }

    if ($parts.Count -eq 0) {
        $parts.Add($ErrorRecord.Exception.Message)
    }

    return ($parts -join ': ')
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

        $detail = Get-CloudflareErrorDetail -ErrorRecord $_
        Write-Output 'Cloudflare API request failed.'
        Write-Output "  Method: $Method"
        Write-Output "  Endpoint: $(Get-CloudflareLogUri -Uri $Uri)"
        Write-Output "  Detail: $detail"

        throw 'Cloudflare API request failed. See log details above.'
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
