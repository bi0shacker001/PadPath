param(
    [string]$InnoCompiler = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
)

$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'publish.ps1') -Output (Join-Path $PSScriptRoot '..\dist')
if (-not (Test-Path $InnoCompiler)) { throw "Inno Setup 6 was not found at $InnoCompiler. Install it from https://jrsoftware.org/isdl.php" }
& $InnoCompiler (Join-Path $PSScriptRoot '..\installer\PadPath.iss')
