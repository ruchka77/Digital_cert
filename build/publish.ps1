# Publishes a self-contained x64 build to artifacts\DigitalCertHelper (no .NET installation needed on clients).
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root 'artifacts\DigitalCertHelper'

dotnet test (Join-Path $root 'DigitalCertHelper.sln') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }

if (Test-Path $output) { Remove-Item $output -Recurse -Force }
dotnet publish (Join-Path $root 'src\DigitalCertHelper\DigitalCertHelper.csproj') `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=embedded -o $output
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
Write-Host "Published to $output"
