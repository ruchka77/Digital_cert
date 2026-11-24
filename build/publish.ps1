# Tests, then publishes a clean single-file build to artifacts\DigitalCertHelper.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root 'artifacts\DigitalCertHelper'
$solution = Join-Path $root 'DigitalCertHelper.sln'

dotnet test $solution -c Release
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }

if (Test-Path $output) { Remove-Item $output -Recurse -Force }

# The settings come from Properties\PublishProfiles\win-x64-self-contained.pubxml.
dotnet publish $solution -c Release
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
Write-Host "Published to $output"
