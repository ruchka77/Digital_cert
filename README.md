# Digital Certificate Renewal Helper

A Windows desktop tool for Help Desk staff. It prepares a digital certificate renewal request for a
remote access user and opens it as a new Outlook message. **It never sends email**; the Help Desk user reviews
the message in Outlook and clicks Send there.

The user interface is in Hebrew (right-to-left) and uses standard Windows controls only.

## Using the application

1. Click **בחר קובץ** (Select file) and choose the file to attach. Any file type is accepted.
2. Type at least 3 characters of the user's first or last name (Hebrew or English) and pick a suggestion,
   or click **חיפוש** (Search) and double-click a user in the results (Enter and **בחר משתמש** also work).
3. Click **תצוגה מקדימה** (Preview). The user is re-read from Active Directory and validated.
4. Check the preview. Click **חזרה** (Back) to correct something, or **פתח הודעת דואר חדשה**
   (Open a new mail message). The application does not send anything.
5. Review the message in Outlook and click **Send** there.

## Requirements

- Windows 10/11 x64, with the user signed in to a domain account that can read `moia.gov.il`
- Classic Microsoft Outlook (version 16) with a configured profile. The "new Outlook" has no COM support.
- The application and Outlook must both run non-elevated

## Project structure (`src/DigitalCertHelper`)

| Folder | Contents |
|---|---|
| `Application` | Service interfaces, settings, dependency injection |
| `Domain` | Email template, search input rules, error codes |
| `Infrastructure` | Active Directory (System.DirectoryServices) and Outlook (COM interop) |
| `Services` | File and user validation, dialogs, centralized error handling |
| `ViewModels`, `Views`, `Models` | MVVM user interface |
| `Helpers` | LDAP filter escaping, RTL HTML, STA thread |
| `Logging` | Serilog configuration, created in the background at start-up |

Tests are in `tests/DigitalCertHelper.Tests`. Start-up measurements are in [docs/PERFORMANCE.md](docs/PERFORMANCE.md).

## Configuration

The defaults are compiled in, so no configuration file is needed. To change them, place a complete
`appsettings.json` (a sample is in `src\DigitalCertHelper`) beside the executable; it replaces the defaults.

| Setting | Default | Meaning |
|---|---|---|
| `Directory:Domain` | `moia.gov.il` | Active Directory domain |
| `Directory:TimeoutSeconds` | 20 | Query timeout |
| `Directory:SuggestionDebounceMilliseconds` | 500 | Delay before suggestions |
| `Directory:MaxResults` | 200 | Maximum rows in the results grid |
| `Certificate:MaxFileSizeMegabytes` | 10 | Maximum attachment size (file type is not restricted) |
| `Logging:Directory` | `Logs` | Relative to the executable, or absolute |
| `Logging:RetainedFileCount` | 31 | Daily log files to keep |

Recipient, subject and body are compiled in (`Domain/EmailTemplate.cs`), so that editing a configuration
file cannot redirect the email. Earlier versions had a `Certificate:AllowedExtensions` setting; it is ignored.

## Behavior notes

- The user identifier is the national ID (תעודת זהות), read from the `extensionAttribute1` attribute in Active Directory.
  It is shown in the results, the selected-user line and the email. `sAMAccountName` is not used. A user whose
  `extensionAttribute1` is empty is rejected with a message, and no request can be created.
- A user whose enabled/disabled state cannot be read fails validation.
- `.pfx`/`.p12` files and files containing a PEM private key show a warning, because they can disclose a private key.
- Outlook may warn about, and recipients' mail systems may block, some attachment types such as `.cer`, `.crt` and `.exe`.
- The message is HTML, so that the Hebrew text is right-to-left. The message is also saved to Drafts.
- Messages shown to the user are in Hebrew. Log files are in English, for administrators.

## Security and privacy

- No stored credentials. Queries use the signed-in Windows user with LDAP signing and sealing.
- User input is validated and LDAP-escaped.
- Technical error details are written only to the log, never displayed.
- Logs identify users by AD object GUID, not by name or ID number; search text is not logged. File names
  and, for errors, exception details are logged.
- A unit test fails if the assembly ever references an Outlook send operation.

Build: [docs/BUILD.md](docs/BUILD.md). Deployment: [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md).
