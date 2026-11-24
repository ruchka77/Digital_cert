# Digital Certificate Renewal Helper

A Windows desktop tool for Help Desk staff. It prepares a digital certificate renewal request for a
remote access user and opens it as an Outlook draft. **It never sends email**; the Help Desk user reviews
the draft in Outlook and clicks Send.

## Using the application

1. Click **Select Certificate** and choose the file.
2. Type at least 3 characters of the user's first or last name (Hebrew or English) and pick a suggestion,
   or click **Search** and double-click a user in the results (Enter and **Select User** also work).
3. Click **Preview Email**. The user is re-read from Active Directory and validated.
4. Check the preview. Click **Back** to correct something, or **Create Outlook Draft**.
5. Review the message in Outlook and click **Send** there.

## Requirements

- Windows 10/11 x64, with the user signed in to a domain account that can read `moia.gov.il`
- Classic Microsoft Outlook (version 16) with a configured profile. The "new Outlook" has no COM support.
- The application and Outlook must both run non-elevated

## Project structure (`src/DigitalCertHelper`)

| Folder | Contents |
|---|---|
| `Application` | Service interfaces, options, dependency injection |
| `Domain` | Email template, search input rules, error codes |
| `Infrastructure` | Active Directory (System.DirectoryServices) and Outlook (COM interop) |
| `Services` | Certificate and user validation, dialogs, centralized error handling |
| `ViewModels`, `Views`, `Models` | MVVM user interface |
| `Helpers` | LDAP filter escaping, RTL HTML, STA thread |
| `Logging` | Serilog configuration |

Tests are in `tests/DigitalCertHelper.Tests`.

## Configuration (`appsettings.json`)

The defaults are embedded in the executable. A complete `appsettings.json` placed beside it replaces them.

| Setting | Default | Meaning |
|---|---|---|
| `Directory:Domain` | `moia.gov.il` | Active Directory domain |
| `Directory:TimeoutSeconds` | 20 | Query timeout |
| `Directory:SuggestionDebounceMilliseconds` | 500 | Delay before suggestions |
| `Directory:MaxResults` | 200 | Maximum rows in the results grid |
| `Certificate:AllowedExtensions` | common certificate types | File types that may be attached |
| `Certificate:MaxFileSizeMegabytes` | 10 | Maximum attachment size |
| `Logging:Directory` | `Logs` | Relative to the executable, or absolute |
| `Logging:RetainedFileCount` | 31 | Daily log files to keep |

Recipient, subject and body are compiled in (`Domain/EmailTemplate.cs`), so that editing a configuration
file cannot redirect certificates.

## Behavior notes

- **User Logon Name** is the `sAMAccountName` attribute (as in `mberger`). It is printed in the email as
  תעודת זהות, so it must hold the ID number. To use another attribute, change `ActiveDirectoryService`.
- A user whose enabled/disabled state cannot be read fails validation.
- `.pfx`/`.p12` files and PEM private keys show a warning, because they can disclose a private key.
- Outlook may warn about, and recipients' mail systems may block, attachments such as `.cer` and `.crt`.
- The message is HTML, so that the Hebrew text is right-to-left. The draft is also saved to Drafts.

## Security and privacy

- No stored credentials. Queries use the signed-in Windows user with LDAP signing and sealing.
- User input is validated and LDAP-escaped.
- Technical error details are written only to the log, never displayed.
- Logs identify users by AD object GUID, not by name or ID number; search text is not logged. Certificate
  file names and, for errors, exception details are logged.
- A unit test fails if the assembly ever references an Outlook send operation.

Build: [docs/BUILD.md](docs/BUILD.md). Deployment: [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md).
