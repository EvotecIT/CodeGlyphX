param(
    [string] $ConfigPath = "$PSScriptRoot\project.build.json",
    [Nullable[bool]] $UpdateVersions = $true,
    [Nullable[bool]] $Build = $true,
    [Nullable[bool]] $Plan,
    [string] $PlanPath
)

$invokeParams = @{
    ConfigPath = $ConfigPath
    UpdateVersions = $UpdateVersions
    Build = $Build
    PublishNuget = $false
    PublishGitHub = $false
}
if ($null -ne $Plan) { $invokeParams.Plan = $Plan }
if ($PlanPath) { $invokeParams.PlanPath = $PlanPath }

& "$PSScriptRoot\Build-Project.ps1" @invokeParams
