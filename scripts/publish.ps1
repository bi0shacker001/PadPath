param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string]$Runtime = 'win-x64',
    [string]$Output = (Join-Path $PSScriptRoot '..\dist')
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\src\HandheldLauncher\HandheldLauncher.csproj'
dotnet publish $project -c Release -r $Runtime --self-contained true -p:PublishSingleFile=true -o $Output
Copy-Item (Join-Path $PSScriptRoot '..\config.example.json') (Join-Path $Output 'config.example.json') -Force
Write-Host "Portable build created at $Output"
