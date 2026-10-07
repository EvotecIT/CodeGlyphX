[CmdletBinding()]
param(
    [switch] $NoBuild
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repositoryRoot 'CodeGlyphX/CodeGlyphX.csproj'
$suppressions = Join-Path $repositoryRoot 'CodeGlyphX/CompatibilitySuppressions.xml'
$arguments = @(
    'pack', $project, '-c', 'Release', '--nologo',
    '-o', (Join-Path $repositoryRoot 'artifacts/compatibility'),
    '-p:ApiCompatGenerateSuppressionFile=true',
    "-p:ApiCompatSuppressionOutputFile=$suppressions"
)
if ($NoBuild) { $arguments += '--no-build' }

# Run explicitly after reviewing an intentional API break; ordinary pack never refreshes this file.
# Review the generated diagnostic IDs, symbol identities, and baseline asset pairs before committing.
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw "API compatibility refresh failed with exit code $LASTEXITCODE." }
Write-Host "Review changes in $suppressions and rerun ordinary dotnet pack to verify the baseline."
