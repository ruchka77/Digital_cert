using System.Text.Json;

namespace DigitalCertHelper.Application;

/// <summary>
/// Built-in defaults, replaced by an appsettings.json beside the executable when one exists.
/// The file is only parsed when present, so a normal start loads no configuration libraries at all.
/// </summary>
public sealed class AppSettings
{
    private const string FileName = "appsettings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public DirectoryOptions Directory { get; set; } = new();

    public CertificateOptions Certificate { get; set; } = new();

    public LoggingOptions Logging { get; set; } = new();

    public static AppSettings Load(string baseDirectory)
    {
        var path = Path.Combine(baseDirectory, FileName);
        var settings = File.Exists(path)
            ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOptions)
                ?? throw new InvalidOperationException("appsettings.json is empty.")
            : new AppSettings();

        settings.Directory.Validate();
        settings.Certificate.Validate();
        return settings;
    }
}
