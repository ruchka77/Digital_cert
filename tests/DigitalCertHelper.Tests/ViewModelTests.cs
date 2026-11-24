using DigitalCertHelper.Application;
using DigitalCertHelper.Domain;
using DigitalCertHelper.Services;
using DigitalCertHelper.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DigitalCertHelper.Tests;

public sealed class ViewModelTests
{
    private readonly FakeDirectory _directory = new();
    private readonly FakeDialogs _dialogs = new();
    private readonly FakeCertificates _certificates = new();

    private ErrorHandler Errors => new(_dialogs, NullLogger<ErrorHandler>.Instance);

    private MainViewModel Create(bool withCertificate = true)
    {
        var viewModel = new MainViewModel(
            _certificates, _directory,
            new UserValidationService(_directory, NullLogger<UserValidationService>.Instance),
            _dialogs, Errors,
            new DirectoryOptions { SuggestionDebounceMilliseconds = 100 },
            NullLogger<MainViewModel>.Instance);
        if (withCertificate)
        {
            viewModel.SelectCertificateCommand.Execute(null);
        }

        return viewModel;
    }

    [Fact]
    public void Nothing_is_available_before_a_certificate_is_selected()
    {
        var viewModel = Create(withCertificate: false);
        Assert.False(viewModel.SearchCommand.CanExecute(null));
        Assert.False(viewModel.PreviewEmailCommand.CanExecute(null));

        _dialogs.File = null;
        viewModel.SelectCertificateCommand.Execute(null);
        Assert.False(viewModel.HasCertificate);
    }

    [Fact]
    public async Task Typing_is_debounced_to_the_last_text()
    {
        _directory.Users = [Sample.User()];
        var viewModel = Create();
        viewModel.SearchText = "Mich";
        var first = viewModel.SuggestionTask;
        viewModel.SearchText = "Micha";
        await Task.WhenAll(first, viewModel.SuggestionTask);

        Assert.Equal(["Micha"], _directory.Terms);
        Assert.True(viewModel.IsSuggestionsOpen);
    }

    [Fact]
    public async Task Suggestions_start_at_three_characters_and_are_limited_to_fifteen()
    {
        _directory.Users = Enumerable.Range(0, 20).Select(_ => Sample.User()).ToList();
        var viewModel = Create();
        viewModel.SearchText = "Mi";
        await viewModel.SuggestionTask;
        Assert.Empty(_directory.Terms);

        viewModel.SearchText = "Mic";
        await viewModel.SuggestionTask;
        Assert.Equal(15, viewModel.Suggestions.Count);
    }

    [Fact]
    public async Task Suggestion_failures_do_not_open_dialogs()
    {
        _directory.Error = new AppException(ErrorCode.DirectoryUnavailable);
        var viewModel = Create();
        viewModel.SearchText = "Mich";
        await viewModel.SuggestionTask;
        Assert.Empty(_dialogs.Errors);
        Assert.Contains("could not be reached", viewModel.Status);
    }

    [Fact]
    public async Task Accepted_suggestion_leads_to_preview()
    {
        var user = Sample.User();
        _directory.Users = [user];
        var viewModel = Create();
        viewModel.AcceptSuggestionCommand.Execute(user);
        Assert.Same(user, viewModel.SelectedUser);

        await viewModel.PreviewEmailCommand.ExecuteAsync(null);
        Assert.NotNull(_dialogs.Previewed);
        Assert.Empty(_directory.Terms);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task New_search_clears_the_previous_selection()
    {
        var user = Sample.User();
        _directory.Users = [user];
        var viewModel = Create();
        viewModel.AcceptSuggestionCommand.Execute(user);
        await viewModel.SearchCommand.ExecuteAsync(null);
        Assert.Null(viewModel.SelectedUser);

        await viewModel.PreviewEmailCommand.ExecuteAsync(null);
        Assert.Equal(["No user selected."], _dialogs.Errors);
        Assert.Null(_dialogs.Previewed);
    }

    [Fact]
    public async Task Missing_certificate_stops_the_preview()
    {
        var viewModel = Create();
        _certificates.Error = new AppException(ErrorCode.CertificateMissing);
        await viewModel.PreviewEmailCommand.ExecuteAsync(null);
        Assert.Single(_dialogs.Errors);
        Assert.Null(_dialogs.Previewed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Preview_closes_only_when_the_draft_was_created(bool succeeds)
    {
        var outlook = new FakeOutlook { Error = succeeds ? null : new AppException(ErrorCode.OutlookUnavailable) };
        var viewModel = new EmailPreviewViewModel(
            EmailTemplate.Create("x", "y", Sample.Certificate), outlook, _certificates, Errors,
            NullLogger<EmailPreviewViewModel>.Instance);
        bool? closed = null;
        viewModel.CloseRequested += (_, created) => closed = created;

        await viewModel.CreateOutlookDraftCommand.ExecuteAsync(null);

        Assert.Equal(1, outlook.Calls);
        Assert.Equal(succeeds ? true : (bool?)null, closed);
        Assert.Equal(succeeds ? 0 : 1, _dialogs.Errors.Count);
    }
}
