Import-Module PSPublishModule -Force

Get-ProjectVersion -Path "C:\Support\GitHub\CodeMatrix" -ExcludeFolders @('C:\Support\GitHub\CodeMatrix\Module\Artefacts') | Format-Table

Set-ProjectVersion -Path "C:\Support\GitHub\CodeMatrix" -NewVersion "1.1.0" -Verbose -ExcludeFolders @('C:\Support\GitHub\CodeMatrix\Module\Artefacts')