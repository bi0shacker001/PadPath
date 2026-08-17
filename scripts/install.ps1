param(
    [string]$Source = (Join-Path $PSScriptRoot '..\dist'),
    [string]$Destination = (Join-Path $env:LOCALAPPDATA 'PadPath')
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path (Join-Path $Source 'PadPath.exe'))) { throw 'Run scripts\publish.ps1 first.' }
New-Item -ItemType Directory -Path $Destination -Force | Out-Null
Copy-Item (Join-Path $Source '*') $Destination -Recurse -Force
if (-not (Test-Path (Join-Path $Destination 'config.json'))) {
    Copy-Item (Join-Path $Destination 'config.example.json') (Join-Path $Destination 'config.json')
}
Write-Host "Installed to $Destination"
Write-Host 'Add PadPath.exe to Steam with Games > Add a Non-Steam Game to My Library.'
