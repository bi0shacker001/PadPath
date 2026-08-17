param(
    [ValidateSet('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')]
    [string]$Runtime = 'win-x64',
    [string]$Output = (Join-Path $PSScriptRoot '..\dist')
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\src\PadPath\PadPath.csproj'
dotnet publish $project -c Release -r $Runtime --self-contained true -p:PublishSingleFile=true -o $Output
Copy-Item (Join-Path $PSScriptRoot '..\config.example.json') (Join-Path $Output 'config.example.json') -Force
if ($Runtime.StartsWith('linux-')) { Copy-Item (Join-Path $PSScriptRoot 'install.sh') (Join-Path $Output 'install.sh') -Force }
if ($Runtime.StartsWith('osx-')) { Copy-Item (Join-Path $PSScriptRoot 'install-macos.sh') (Join-Path $Output 'install-macos.sh') -Force }
Write-Host "Portable build created at $Output"
