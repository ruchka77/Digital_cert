using System.Windows;
using DigitalCertHelper.Application;
using DigitalCertHelper.Models;
using DigitalCertHelper.ViewModels;
using DigitalCertHelper.Views;
using Microsoft.Win32;

namespace DigitalCertHelper.Services;

public sealed class DialogService(Func<EmailDraft, EmailPreviewViewModel> previewFactory) : IDialogService
{
    private const string Title = "בקשת חידוש תעודה דיגיטלית";

    private const MessageBoxOptions RightToLeft = MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign;

    /// <summary>The topmost dialog, even while another application (such as Outlook) has the focus.</summary>
    private static Window? Owner =>
        System.Windows.Application.Current?.Windows.OfType<Window>().LastOrDefault(w => w.IsVisible);

    public string? PickFile(string title, string filter)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter, // "All files": any file type may be attached
            CheckFileExists = true,
            Multiselect = false,
        };
        return dialog.ShowDialog(Owner) == true ? dialog.FileName : null;
    }

    public void ShowError(string message)
    {
        if (Owner is { } owner)
        {
            MessageBox.Show(owner, message, Title, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK, RightToLeft);
        }
        else
        {
            MessageBox.Show(message, Title, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK, RightToLeft);
        }
    }

    public bool ShowEmailPreview(EmailDraft draft)
    {
        var window = new EmailPreviewWindow(previewFactory(draft)) { Owner = Owner };
        return window.ShowDialog() == true;
    }
}
