using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DigitalCertHelper.Application;
using DigitalCertHelper.Models;
using Microsoft.Extensions.Logging;

namespace DigitalCertHelper.ViewModels;

public sealed partial class EmailPreviewViewModel(
    EmailDraft draft,
    IOutlookService outlook,
    ICertificateService certificates,
    IErrorHandler errors,
    ILogger<EmailPreviewViewModel> logger) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand), nameof(CreateOutlookDraftCommand))]
    private bool _isBusy;

    /// <summary>Raised with true when the draft was opened, false for Back.</summary>
    public event EventHandler<bool>? CloseRequested;

    public EmailDraft Draft { get; } = draft;

    public bool IsIdle => !IsBusy;

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private void Back() => CloseRequested?.Invoke(this, false);

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task CreateOutlookDraftAsync()
    {
        var created = false;
        IsBusy = true;
        try
        {
            logger.LogInformation("Outlook draft requested");
            certificates.Load(Draft.Attachment.FullPath); // may have been removed while previewing
            await outlook.CreateDraftAsync(Draft);
            created = true;
        }
        catch (Exception ex)
        {
            errors.Handle(ex, "Create Outlook draft");
        }
        finally
        {
            IsBusy = false;
        }

        if (created)
        {
            CloseRequested?.Invoke(this, true);
        }
    }
}
