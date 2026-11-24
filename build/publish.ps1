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

# Intermediate files contain a second, uncompressed copy of the .NET runtime (over 130 MB).
$project = Join-Path $root 'src\DigitalCertHelper'
Get-ChildItem (Join-Path $project 'bin\Release'), (Join-Path $project 'obj\Release') -Directory -Recurse -Filter 'win-x64' |
    Remove-Item -Recurse -Force

$exe = Get-Item (Join-Path $output 'DigitalCertHelper.exe')
$size = $exe.Length / 1MB
Write-Host ('{0}: {1:N1} MB' -f $exe.Name, $size)
if ($size -ge 100) { throw 'The executable exceeds 100 MB, the GitHub file size limit.' }
