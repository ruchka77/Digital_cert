# Deployment instructions

1. Run `build\publish.ps1` (see [BUILD.md](BUILD.md)).
2. Sign `DigitalCertHelper.exe` with the organization's code-signing certificate.
3. Copy `DigitalCertHelper.exe` and `appsettings.json` to, for example,
   `C:\Program Files\DigitalCertHelper`, using Intune, SCCM or Group Policy. No .NET installation is needed.
4. Create a Start menu shortcut. Do not run the application as administrator.

Keep the folder read-only for standard users, so that the executable and configuration cannot be altered.

## Logs

Logs are written to `Logs\` beside the executable. If that folder is not writable (normal under Program
Files), they go to `%LOCALAPPDATA%\DigitalCertHelper\Logs`. To keep them beside the executable, create
`Logs` and grant the Help Desk group Modify on that folder only.

## Acceptance test on a domain workstation

- [ ] Suggestions appear for Hebrew and English partial names
- [ ] The User Logon Name shown is the value required as תעודת זהות
- [ ] A disabled account is refused
- [ ] The draft has the correct recipient, subject, right-to-left body and attachment
- [ ] The draft opens both when Outlook is already running and when it is closed
- [ ] A reviewed test message arrives with its attachment (some gateways remove `.cer`/`.crt`)
- [ ] Disconnecting the network gives a clear error message

## Updating and removing

To update, close the application and replace the executable, keeping a customized `appsettings.json`.
To remove, delete the folder, the shortcut and `%LOCALAPPDATA%\DigitalCertHelper`.

## .NET support

Microsoft's support for .NET 8 ended on 10 November 2026. Because the package is self-contained, the runtime
is part of it and is not serviced by Windows Update. Plan an upgrade to .NET 10 LTS: change `TargetFramework`
in `Directory.Build.props`, update the Microsoft packages, test and republish.
