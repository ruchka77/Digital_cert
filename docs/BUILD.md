# Build instructions

Prerequisites: .NET 10 SDK, or Visual Studio 2026 (18.0+) with the **.NET desktop development** workload,
and access to nuget.org or an internal mirror. Outlook is not needed to build or test.

Visual Studio: open `DigitalCertHelper.sln`, set `DigitalCertHelper` as the startup project, press F5.

Command line:

```powershell
dotnet restore DigitalCertHelper.sln
dotnet build DigitalCertHelper.sln -c Release
dotnet test DigitalCertHelper.sln -c Release
```

## Publishing for end users

From the repository root (the folder containing `DigitalCertHelper.sln`) or from `src\DigitalCertHelper`:

```powershell
dotnet publish -c Release
```

Output: **`artifacts\DigitalCertHelper\DigitalCertHelper.exe`**. This one file is everything end users need;
.NET does not have to be installed on their computers. Internet access is needed for the first publish, to
download the .NET runtime pack.

The profile `win-x64-self-contained` (`src\DigitalCertHelper\Properties\PublishProfiles`) is applied
automatically: .NET 10, Windows x64, self-contained, compressed single file. In Visual Studio, right-click
the `DigitalCertHelper` project, choose **Publish**, select that profile and click **Publish**.

Do not add `-o`: .NET does not allow it when publishing a solution.

For releases, use the script, which also runs the tests and clears old output first:

```powershell
powershell -ExecutionPolicy Bypass -File build\publish.ps1
```

## Output size

Only `artifacts\DigitalCertHelper\DigitalCertHelper.exe` (about 58 MB) is for distribution. The `bin` and `obj`
folders are intermediate and hold uncompressed copies of the .NET runtime; git ignores them, and they can be deleted.

Trimming is not used: the SDK rejects it for WPF (NETSDK1168), and it would disable the COM support that Outlook
and Active Directory access need. Instead the executable is compressed, debug symbols are omitted, and a few
unused runtime files are excluded (`ExcludeUnusedRuntimeFiles` in the project file). Log stack traces therefore
show method names without line numbers.

### Uncompressed build (less memory, not for git)

Compression keeps the file under GitHub's limit but costs about 90 MB of memory and 130 ms at every start,
because the files are unpacked in memory. If the executable is deployed by Intune or a file share instead of git,
this variant is smaller in memory (about 100 MB less working set) and faster, but the file is about 120 MB:

```powershell
dotnet publish -c Release -p:EnableCompressionInSingleFile=false
```

See [PERFORMANCE.md](PERFORMANCE.md) for the measurements.

## Committing the executable

`.gitignore` allows that one file from `artifacts`. GitHub warns above 50 MB and rejects files above 100 MB.
Every committed version permanently adds about 55 MB to the repository, so prefer GitHub Releases for new versions.
