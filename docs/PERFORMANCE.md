# Start-up performance report

Measured on one Windows 11 PC, running the published single-file executable alone in an empty folder.
"Start-up" is the time from launching the process until the main window exists. Each figure is the median of
7 warm starts (the first start also unpacks files to `%TEMP%\.net` and is slower). Numbers vary by a few percent.

| | Warm start | First start | Working set | Private memory |
|---|---|---|---|---|
| Before | 800 ms | 930-1,230 ms | 192 MB | 108 MB |
| After | **697 ms** | 850 ms | 193 MB | 124 MB |
| Empty WPF window, same packaging (the floor) | 609 ms | 1,040 ms | 162 MB | 118 MB |

**Result: about 13% faster (100 ms).** Measured against the floor, the application's own share of start-up fell from
about 190 ms to about 90 ms. Memory did not improve (see below).

## What was slow

About 610 ms of the 800 ms is paid by any WPF application packaged this way, and no application change can remove it:
starting the .NET runtime, loading WPF, and unpacking the compressed single file. That leaves about 190 ms that the
application itself added. From reading the code (each item was not timed separately), the main causes were:

1. **Configuration libraries** (`Microsoft.Extensions.Configuration`, JSON provider, binder, file providers) were
   loaded and an embedded JSON file was parsed on every start, only to produce a few default values.
2. **Logging was initialized before the window**: Serilog and the file sink loaded, the log folder was checked for write
   access, and old log files were scanned, all before anything was shown.
3. **The full logging stack** (`AddLogging`) was registered in the dependency injection container.
4. **Visual complexity**: two indeterminate progress bars (templates and animation), a transparent popup window,
   a custom button template and nine custom styles, which all had to be parsed and built.

## What changed

| Change | Effect |
|---|---|
| Defaults are compiled in ([AppSettings.cs](../src/DigitalCertHelper/Application/AppSettings.cs)). `appsettings.json` is parsed only if it exists. The two configuration packages were removed. | No configuration libraries are loaded on a normal start |
| Logging is created on a background thread ([BackgroundLoggerFactory.cs](../src/DigitalCertHelper/Logging/BackgroundLoggerFactory.cs)). A log entry waits only if that is still unfinished. | The window no longer waits for Serilog, and there is no splash screen |
| The container registers only what is used, with the lightweight logger registration instead of `AddLogging`. `MainWindow` is created directly. | Less work and fewer loaded assemblies |
| Windows use standard controls only: no styles, templates, progress bars, animations or transparent popup | Smaller visual tree, nothing animated |
| The certificate check reads only the first 64 KB of a file, to look for a private key | A large attachment no longer loads fully into memory |
| Publish settings (see below) | Smaller file |

**Already lazy, and unchanged:** the Active Directory service creates no connection until a search runs, and the
Outlook service starts nothing until "פתח הודעת דואר חדשה" is clicked. Both are constructed on first use, and
neither does work in its constructor. The Outlook types are embedded in the program, so no Office assembly loads at start-up.

## What did not help

| Tried | Result |
|---|---|
| `InvariantGlobalization` | **Breaks WPF**: data binding fails with "Cannot find non-neutral culture". Rejected. |
| Concurrent GC off, tiered PGO off | No measurable difference |
| ReadyToRun (precompiled code) | 8% faster with the old start-up code, but no difference after the changes above, while adding 5 MB and about 19 MB of private memory. Turned off. |
| Trimming | The SDK rejects it for WPF |

## Memory, and the one big trade-off

Memory did not improve: the working set is the same, and private memory is about 15 MB higher. I did not find the
cause of the increase; a likely candidate is the background logging thread, which starts at the same time as the window.

The largest single factor is the compressed single file. Unpacking it costs about 90 MB of memory and about 130 ms at
every start. Measured on the earlier code, an uncompressed build started in 664 ms with a 101 MB working set, but the
file was 122 MB, which is above GitHub's 100 MB limit. The default therefore stays compressed. If the executable is
deployed with Intune or a file share rather than git, publish the uncompressed variant described in
[BUILD.md](BUILD.md#uncompressed-build-less-memory-not-for-git); I would expect it to start in about 560 ms.
That final figure is an estimate: it was not measured on the final code.

## Release size

The executable is 58 MB (the previous version was 62 MB): compressed, no debug symbols, and five unused runtime files removed.
