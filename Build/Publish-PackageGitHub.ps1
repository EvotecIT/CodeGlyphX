$GitHubAccessToken = Get-Content -Raw 'C:\Support\Important\GithubAPI.txt'

$publishGitHubReleaseAssetSplat = @{
    ProjectPath          = "$PSScriptRoot\..\CodeGlyphX"
    GitHubAccessToken    = $GitHubAccessToken
    GitHubUsername       = "EvotecIT"
    GitHubRepositoryName = "CodeGlyphX"
    IsPreRelease         = $false
    GenerateReleaseNotes = $true
}

Publish-GitHubReleaseAsset @publishGitHubReleaseAssetSplat
