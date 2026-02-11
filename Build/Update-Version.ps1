param(
    [string] $ConfigPath = "$PSScriptRoot\project.build.json",
    [Nullable[bool]] $Plan,
    [string] $PlanPath
)

$invokeParams = @{
    ConfigPath = $ConfigPath
    UpdateVersions = $true
    Build = $false
    PublishNuget = $false
    PublishGitHub = $false
}
if ($null -ne $Plan) { $invokeParams.Plan = $Plan }
if ($PlanPath) { $invokeParams.PlanPath = $PlanPath }

& "$PSScriptRoot\Build-Project.ps1" @invokeParams
