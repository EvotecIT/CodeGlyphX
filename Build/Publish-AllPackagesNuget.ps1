param(
    [string] $ConfigPath = "$PSScriptRoot\project.build.json",
    [Nullable[bool]] $Build = $true,
    [Nullable[bool]] $Plan,
    [string] $PlanPath
)

$invokeParams = @{
    ConfigPath = $ConfigPath
    UpdateVersions = $false
    Build = $Build
    PublishNuget = $true
    PublishGitHub = $false
}
if ($null -ne $Plan) {
    $invokeParams.Plan = $Plan
    if ($Plan) {
        Write-Warning "Plan mode generates a build plan only (NuGet publish preflight requires existing staged packages)."
        $invokeParams.PublishNuget = $false
    }
}
if ($PlanPath) { $invokeParams.PlanPath = $PlanPath }

& "$PSScriptRoot\Build-Project.ps1" @invokeParams
