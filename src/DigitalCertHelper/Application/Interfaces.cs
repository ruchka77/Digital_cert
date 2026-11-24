using DigitalCertHelper.Models;

namespace DigitalCertHelper.Application;

public interface IDirectoryService
{
    /// <summary>Finds users whose given name, surname or display name contains every word of the term.</summary>
    Task<DirectorySearchResult> SearchAsync(string term, int maxResults, CancellationToken cancellationToken);

    /// <summary>Re-reads one user; null when the user no longer exists.</summary>
    Task<DirectoryUser?> FindByIdAsync(Guid objectId, CancellationToken cancellationToken);
}

public interface IOutlookService
{
    /// <summary>Creates and displays a draft. There is intentionally no way to send through this interface.</summary>
    Task CreateDraftAsync(EmailDraft draft);
}

public interface ICertificateService
{
    string FileDialogFilter { get; }

    /// <summary>Checks that the file is usable; throws an AppException if not.</summary>
    CertificateFile Load(string path);
}

public interface IUserValidationService
{
    Task<UserValidationResult> ValidateAsync(
        IReadOnlyCollection<DirectoryUser> selectedUsers, CancellationToken cancellationToken);
}

public interface IDialogService
{
    string? PickFile(string title, string filter);

    void ShowError(string message);

    /// <summary>Returns true when the Outlook draft was opened, false when the user went back.</summary>
    bool ShowEmailPreview(EmailDraft draft);
}

public interface IErrorHandler
{
    string GetUserMessage(Exception exception);

    /// <summary>Logs the failure and returns a message that is safe to display.</summary>
    string Log(Exception exception, string operation);

    /// <summary>Logs the failure and shows it to the user.</summary>
    void Handle(Exception exception, string operation);
}
