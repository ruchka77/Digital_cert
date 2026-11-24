# Deployment instructions

1. Run `build\publish.ps1` (see [BUILD.md](BUILD.md)).
2. Sign `DigitalCertHelper.exe` with the organization's code-signing certificate.
3. Copy `DigitalCertHelper.exe` to, for example, `C:\Program Files\DigitalCertHelper`, using Intune, SCCM
   or Group Policy. No other file and no .NET installation is needed. To override settings, also copy a
   complete, edited `appsettings.json` (from `src\DigitalCertHelper`) beside it.
4. Create a Start menu shortcut. Do not run the application as administrator.

Keep the folder read-only for standard users, so that the executable and configuration cannot be altered.

## Logs

Logs are written to `Logs\` beside the executable. If that folder is not writable (normal under Program
Files), they go to `%LOCALAPPDATA%\DigitalCertHelper\Logs`. To keep them beside the executable, create
`Logs` and grant the Help Desk group Modify on that folder only.

## Acceptance test on a domain workstation

- [ ] The windows open right-to-left, and every label, button and message is in Hebrew
- [ ] The file picker shows all files, and a non-certificate file (for example a PDF) can be attached
- [ ] Suggestions appear for Hebrew and English partial names
- [ ] The User Logon Name shown is the value required as תעודת זהות
- [ ] A disabled account is refused
- [ ] The draft has the correct recipient, subject, right-to-left body and attachment
- [ ] 'פתח הודעת דואר חדשה' opens a new message in Outlook both when Outlook is already running and when it is closed,
      and nothing is sent until Send is clicked in Outlook
- [ ] A reviewed test message arrives with its attachment (some gateways remove `.cer`/`.crt`)
- [ ] Disconnecting the network gives a clear error message

## Updating and removing

To update, close the application and replace the executable, keeping a customized `appsettings.json`
(compare it with the sample: settings that no longer exist are ignored).
To remove, delete the folder, the shortcut and `%LOCALAPPDATA%\DigitalCertHelper`.

On first start, the executable extracts its native libraries to `%TEMP%\.net\DigitalCertHelper`. If application
control (AppLocker or WDAC) blocks DLLs there, allow them, or publish with
`-p:PublishSingleFile=false -p:EnableCompressionInSingleFile=false` and deploy the whole output folder.

## .NET support

The application targets .NET 10 LTS, supported until November 2028. Because the package is self-contained, the
runtime is part of it and is not serviced by Windows Update. Republish with the latest .NET 10 SDK and redeploy
regularly (Microsoft releases security updates monthly).
