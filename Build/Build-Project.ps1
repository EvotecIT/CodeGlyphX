param(
    [string] $ConfigPath = "$PSScriptRoot\project.build.json",
    [object] $UpdateVersions,
    [object] $Build,
    [object] $PublishNuget,
    [object] $PublishGitHub,
    [object] $Plan,
    [string] $PlanPath
)

Import-Module PSPublishModule -Force -ErrorAction Stop

function ConvertTo-NullableBool {
    param(
        [object] $Value,
        [string] $ParameterName
    )

    if ($null -eq $Value) {
        return $null
    }

    if ($Value -is [System.Management.Automation.SwitchParameter]) {
        return [bool] $Value.IsPresent
    }

    if ($Value -is [bool]) {
        return [bool] $Value
    }

    if ($Value -is [int]) {
        switch ([int] $Value) {
            0 { return $false }
            1 { return $true }
            default { throw "Parameter '$ParameterName' accepts only True/False or 1/0. Got: $Value" }
        }
    }

    if ($Value -is [string]) {
        switch ($Value.Trim().ToLowerInvariant()) {
            'true' { return $true }
            'false' { return $false }
            '1' { return $true }
            '0' { return $false }
            'yes' { return $true }
            'no' { return $false }
            'on' { return $true }
            'off' { return $false }
            default { throw "Parameter '$ParameterName' accepts only True/False or 1/0. Got: '$Value'" }
        }
    }

    throw "Parameter '$ParameterName' accepts only True/False or 1/0. Got type: $($Value.GetType().FullName)"
}

$invokeParams = @{
    ConfigPath = $ConfigPath
}
if ($PSBoundParameters.ContainsKey('UpdateVersions')) {
    $invokeParams.UpdateVersions = ConvertTo-NullableBool -Value $UpdateVersions -ParameterName 'UpdateVersions'
}
if ($PSBoundParameters.ContainsKey('Build')) {
    $invokeParams.Build = ConvertTo-NullableBool -Value $Build -ParameterName 'Build'
}
if ($PSBoundParameters.ContainsKey('PublishNuget')) {
    $invokeParams.PublishNuget = ConvertTo-NullableBool -Value $PublishNuget -ParameterName 'PublishNuget'
}
if ($PSBoundParameters.ContainsKey('PublishGitHub')) {
    $invokeParams.PublishGitHub = ConvertTo-NullableBool -Value $PublishGitHub -ParameterName 'PublishGitHub'
}
if ($PSBoundParameters.ContainsKey('Plan')) {
    $invokeParams.Plan = ConvertTo-NullableBool -Value $Plan -ParameterName 'Plan'
}
if ($PlanPath) { $invokeParams.PlanPath = $PlanPath }

Invoke-ProjectBuild @invokeParams

