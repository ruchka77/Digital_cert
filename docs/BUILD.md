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
