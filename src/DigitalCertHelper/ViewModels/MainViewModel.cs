using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DigitalCertHelper.Application;
using DigitalCertHelper.Domain;
using DigitalCertHelper.Models;
using Microsoft.Extensions.Logging;

namespace DigitalCertHelper.ViewModels;

public sealed partial class MainViewModel(
    ICertificateService certificates,
    IDirectoryService directory,
    IUserValidationService validation,
    IDialogService dialogs,
    IErrorHandler errors,
    DirectoryOptions options,
    ILogger<MainViewModel> logger) : ObservableObject
{
    public const int MaxSuggestions = 15;

    private CancellationTokenSource? _suggestionCancellation;
    private bool _suppressSuggestions;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCertificate), nameof(IsSearchEnabled))]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand), nameof(PreviewEmailCommand))]
    private CertificateFile? _certificate;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isSuggestionsOpen;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectUserCommand))]
    private DirectoryUser? _highlightedUser;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedUser))]
    private DirectoryUser? _selectedUser;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle), nameof(IsSearchEnabled))]
    [NotifyCanExecuteChangedFor(
        nameof(SelectCertificateCommand), nameof(SearchCommand), nameof(SelectUserCommand),
        nameof(PreviewEmailCommand), nameof(NewRequestCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _status = "בחר קובץ כדי להתחיל.";

    public ObservableCollection<DirectoryUser> Suggestions { get; } = [];

    public ObservableCollection<DirectoryUser> Results { get; } = [];

    public bool HasCertificate => Certificate is not null;

    public bool HasSelectedUser => SelectedUser is not null;

    public bool IsIdle => !IsBusy;

    public bool IsSearchEnabled => HasCertificate && IsIdle;

    /// <summary>The latest suggestion lookup; exposed for tests.</summary>
    internal Task SuggestionTask { get; private set; } = Task.CompletedTask;

    private bool CanSelectUser => IsIdle && HighlightedUser is not null;

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private void SelectCertificate()
    {
        try
        {
            var path = dialogs.PickFile("בחירת קובץ", certificates.FileDialogFilter);
            if (path is null)
            {
                logger.LogInformation("Certificate selection cancelled");
                return;
            }

            Certificate = certificates.Load(path);
            Status = "הקובץ נבחר. חפש את המשתמש.";
            logger.LogInformation(
                "Certificate selected: {FileName}, may contain private key: {MayContainPrivateKey}",
                Certificate.FileName, Certificate.MayContainPrivateKey);
        }
        catch (Exception ex)
        {
            errors.Handle(ex, "Select certificate");
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        if (!_suppressSuggestions)
        {
            SuggestionTask = UpdateSuggestionsAsync(value);
        }
    }

    private async Task UpdateSuggestionsAsync(string text)
    {
        CancelSuggestions();
        if (!IsSearchEnabled
            || !SearchTerm.TryNormalize(text, out var term, out _)
            || term.Length < SearchTerm.SuggestionMinimumLength)
        {
            return;
        }

        using var cancellation = new CancellationTokenSource();
        _suggestionCancellation = cancellation;
        try
        {
            await Task.Delay(options.SuggestionDebounceMilliseconds, cancellation.Token);
            var result = await directory.SearchAsync(term, MaxSuggestions, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();

            Replace(Suggestions, result.Users.Take(MaxSuggestions));
            IsSuggestionsOpen = Suggestions.Count > 0;
            Status = Suggestions.Count == 0 ? "לא נמצאו משתמשים מתאימים." : "בחר הצעה או לחץ על 'חיפוש'.";
        }
        catch (OperationCanceledException)
        {
            // Superseded by newer input.
        }
        catch (Exception ex)
        {
            // Never interrupt typing with a dialog.
            var message = errors.Log(ex, "Suggest users");
            if (!cancellation.IsCancellationRequested)
            {
                Status = message;
            }
        }
        finally
        {
            if (ReferenceEquals(_suggestionCancellation, cancellation))
            {
                _suggestionCancellation = null;
            }
        }
    }

    private void CancelSuggestions()
    {
        _suggestionCancellation?.Cancel();
        _suggestionCancellation = null;
        IsSuggestionsOpen = false;
        Suggestions.Clear();
    }

    [RelayCommand]
    private void AcceptSuggestion(DirectoryUser? user)
    {
        if (user is null || !IsSearchEnabled)
        {
            return;
        }

        CancelSuggestions();
        SetSearchText(user.DisplayName);
        Replace(Results, [user]);
        Confirm(user);
    }

    [RelayCommand(CanExecute = nameof(IsSearchEnabled))]
    private async Task SearchAsync()
    {
        CancelSuggestions();
        if (!SearchTerm.TryNormalize(SearchText, out var term, out var error))
        {
            logger.LogWarning("Search input rejected: {Reason}", error);
            dialogs.ShowError(error);
            return;
        }

        IsBusy = true;
        try
        {
            SelectedUser = null;
            HighlightedUser = null;
            Results.Clear();
            Status = "מחפש…";
            var result = await directory.SearchAsync(term, options.MaxResults, CancellationToken.None);
            Replace(Results, result.Users);
            Status = Results.Count == 0
                ? "המשתמש לא נמצא ב-Active Directory."
                : result.IsTruncated
                    ? $"מוצגים {Results.Count} המשתמשים הראשונים. צמצם את החיפוש."
                    : $"נמצאו {Results.Count} משתמשים. לחץ פעמיים על משתמש כדי לבחור אותו.";
        }
        catch (Exception ex)
        {
            Status = "החיפוש נכשל.";
            errors.Handle(ex, "Search users");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSelectUser))]
    private void SelectUser()
    {
        if (HighlightedUser is { } user)
        {
            Confirm(user);
        }
    }

    private void Confirm(DirectoryUser user)
    {
        HighlightedUser = user;
        SelectedUser = user;
        Status = "המשתמש נבחר. לחץ על 'תצוגה מקדימה' כדי להמשיך.";
        logger.LogInformation("User selected: {ObjectId}", user.ObjectId);
    }

    [RelayCommand(CanExecute = nameof(IsSearchEnabled))]
    private async Task PreviewEmailAsync()
    {
        if (Certificate is null)
        {
            return;
        }

        CancelSuggestions();
        IsBusy = true;
        try
        {
            Status = "מאמת…";
            var certificate = certificates.Load(Certificate.FullPath);
            var result = await validation.ValidateAsync(
                SelectedUser is null ? [] : [SelectedUser], CancellationToken.None);
            if (!result.IsValid || result.User is not { } user)
            {
                Status = "האימות נכשל.";
                dialogs.ShowError(string.Join(Environment.NewLine, result.Errors));
                return;
            }

            var displayName = user.DisplayName;
            var nationalId = user.NationalId;
            var draft = EmailTemplate.Create(displayName, nationalId, certificate);

            logger.LogInformation("Email preview opened for {ObjectId}", user.ObjectId);
            var created = dialogs.ShowEmailPreview(draft);
            logger.LogInformation("Email preview closed; draft created: {Created}", created);
            Status = created
                ? "ההודעה נפתחה ב-Outlook. בדוק אותה ושלח אותה משם באופן ידני."
                : "בצע את התיקונים הנדרשים ולחץ שוב על 'תצוגה מקדימה'.";
        }
        catch (Exception ex)
        {
            Status = "לא ניתן היה להכין את ההודעה.";
            errors.Handle(ex, "Prepare email");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private void NewRequest()
    {
        CancelSuggestions();
        SetSearchText(string.Empty);
        Results.Clear();
        HighlightedUser = null;
        SelectedUser = null;
        Certificate = null;
        Status = "בחר קובץ כדי להתחיל.";
    }

    private void SetSearchText(string text)
    {
        _suppressSuggestions = true;
        SearchText = text;
        _suppressSuggestions = false;
    }

    private static void Replace(ObservableCollection<DirectoryUser> target, IEnumerable<DirectoryUser> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }
}
