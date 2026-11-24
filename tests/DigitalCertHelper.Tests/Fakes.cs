using DigitalCertHelper.Application;
using DigitalCertHelper.Models;

namespace DigitalCertHelper.Tests;

internal static class Sample
{
    public static readonly CertificateFile Certificate = new(@"C:\certs\a.cer", false);

    public static DirectoryUser User(string name = "מיכאל ברגר", string logon = "012345678", bool? enabled = true) =>
        new(Guid.NewGuid(), name, logon, "IT", enabled);
}

internal sealed class FakeDirectory : IDirectoryService
{
    public List<string> Terms { get; } = [];
    public List<DirectoryUser> Users { get; set; } = [];
    public Exception? Error { get; set; }

    public Task<DirectorySearchResult> SearchAsync(string term, int maxResults, CancellationToken cancellationToken)
    {
        Terms.Add(term);
        return Error is not null
            ? Task.FromException<DirectorySearchResult>(Error)
            : Task.FromResult(new DirectorySearchResult(Users, false));
    }

    public Task<DirectoryUser?> FindByIdAsync(Guid objectId, CancellationToken cancellationToken) =>
        Task.FromResult(Users.SingleOrDefault(u => u.ObjectId == objectId));
}

internal sealed class FakeDialogs : IDialogService
{
    public List<string> Errors { get; } = [];
    public EmailDraft? Previewed { get; private set; }
    public string? File { get; set; } = Sample.Certificate.FullPath;

    public string? PickFile(string title, string filter) => File;
    public void ShowError(string message) => Errors.Add(message);

    public bool ShowEmailPreview(EmailDraft draft)
    {
        Previewed = draft;
        return true;
    }
}

internal sealed class FakeCertificates : ICertificateService
{
    public Exception? Error { get; set; }
    public string FileDialogFilter => "x|*.cer";
    public CertificateFile Load(string path) => Error is null ? Sample.Certificate : throw Error;
}

internal sealed class FakeOutlook : IOutlookService
{
    public Exception? Error { get; set; }
    public int Calls { get; private set; }

    public Task CreateDraftAsync(EmailDraft draft)
    {
        Calls++;
        return Error is null ? Task.CompletedTask : Task.FromException(Error);
    }
}
