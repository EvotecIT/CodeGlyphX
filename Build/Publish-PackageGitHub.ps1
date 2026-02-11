param(
    [string] $ConfigPath = "$PSScriptRoot\project.build.json",
    [object] $Plan,
    [string] $PlanPath
)

$params = @{
    ConfigPath     = $ConfigPath
    UpdateVersions = $false
    Build          = $false
    PublishNuget   = $false
    PublishGitHub  = $true
}
if ($null -ne $Plan) { $params.Plan = $Plan }
if ($PlanPath) { $params.PlanPath = $PlanPath }

& "$PSScriptRoot\Build-Project.ps1" @params
