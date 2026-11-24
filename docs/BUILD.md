# Build instructions

Prerequisites: .NET 8 SDK, or Visual Studio 2022 (17.8+) with the **.NET desktop development** workload,
and access to nuget.org or an internal mirror. Outlook is not needed to build or test.

Visual Studio: open `DigitalCertHelper.sln`, set `DigitalCertHelper` as the startup project, press F5.

Command line:

```powershell
dotnet restore DigitalCertHelper.sln
dotnet build DigitalCertHelper.sln -c Release
dotnet test DigitalCertHelper.sln -c Release
```

Release package (runs the tests, then publishes a self-contained executable):

```powershell
powershell -ExecutionPolicy Bypass -File build\publish.ps1
```

The output is in `artifacts\DigitalCertHelper`.
