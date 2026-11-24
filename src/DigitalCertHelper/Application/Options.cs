namespace DigitalCertHelper.Application;

public sealed class DirectoryOptions
{
    public string Domain { get; set; } = "moia.gov.il";
    public int TimeoutSeconds { get; set; } = 20;
    public int SuggestionDebounceMilliseconds { get; set; } = 500;
    public int MaxResults { get; set; } = 200;

    public void Validate()
    {
        if (Uri.CheckHostName(Domain) != UriHostNameType.Dns)
        {
            throw new InvalidOperationException("Directory:Domain must be a DNS name.");
        }

        if (TimeoutSeconds is < 1 or > 300 || MaxResults is < 1 or > 999
            || SuggestionDebounceMilliseconds is < 0 or > 10_000)
        {
            throw new InvalidOperationException("A Directory setting is out of range.");
        }
    }
}

public sealed class CertificateOptions
{
    /// <summary>Any file type may be attached; only the size is limited.</summary>
    public int MaxFileSizeMegabytes { get; set; } = 10;

    public void Validate()
    {
        if (MaxFileSizeMegabytes is < 1 or > 100)
        {
            throw new InvalidOperationException("Certificate:MaxFileSizeMegabytes is out of range.");
        }
    }
}

public sealed class LoggingOptions
{
    public string Directory { get; set; } = "Logs";
    public int RetainedFileCount { get; set; } = 31;
}
