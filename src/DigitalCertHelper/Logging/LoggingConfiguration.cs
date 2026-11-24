using DigitalCertHelper.Application;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;

namespace DigitalCertHelper.Logging;

public static class LoggingConfiguration
{
    /// <summary>Runs on a background thread (see <see cref="BackgroundLoggerFactory"/>).</summary>
    public static ILoggerFactory CreateFactory(LoggingOptions options)
    {
        var logger = CreateLogger(options);
        logger.Information("Application starting, version {Version}", typeof(LoggingConfiguration).Assembly.GetName().Version);
        return new SerilogLoggerFactory(logger, dispose: true);
    }

    public static Logger CreateLogger(LoggingOptions options)
    {
        var directory = ResolveDirectory(options.Directory);
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithProperty("ProcessId", Environment.ProcessId)
            .WriteTo.File(
                Path.Combine(directory, "DigitalCertHelper-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: Math.Max(1, options.RetainedFileCount),
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{ProcessId}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    /// <summary>Uses Logs\ beside the executable; falls back to the user profile when that is read-only (Program Files).</summary>
    private static string ResolveDirectory(string configured)
    {
        var preferred = Path.GetFullPath(configured, AppContext.BaseDirectory);
        if (IsWritable(preferred))
        {
            return preferred;
        }

        var fallback = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DigitalCertHelper", "Logs");
        Directory.CreateDirectory(fallback);
        return fallback;
    }

    private static bool IsWritable(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            using var probe = new FileStream(
                Path.Combine(directory, $".{Guid.NewGuid():N}.tmp"),
                FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
