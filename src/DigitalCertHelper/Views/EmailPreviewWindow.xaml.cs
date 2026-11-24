using System.ComponentModel;
using System.Windows;
using DigitalCertHelper.ViewModels;

namespace DigitalCertHelper.Views;

public partial class EmailPreviewWindow : Window
{
    private readonly EmailPreviewViewModel _viewModel;

    public EmailPreviewWindow(EmailPreviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
        viewModel.CloseRequested += (_, created) => DialogResult = created;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Wait for Outlook, so that the result is never lost.
        e.Cancel = _viewModel.IsBusy;
        base.OnClosing(e);
    }
}
