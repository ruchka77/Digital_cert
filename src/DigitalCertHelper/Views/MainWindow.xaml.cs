using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DigitalCertHelper.Models;
using DigitalCertHelper.ViewModels;

namespace DigitalCertHelper.Views;

/// <summary>Contains only input gestures that XAML bindings cannot express.</summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
    }

    private void OnSearchBoxKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down when _viewModel.IsSuggestionsOpen:
                SuggestionList.SelectedIndex = 0;
                SuggestionList.UpdateLayout();
                (SuggestionList.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem)?.Focus();
                e.Handled = true;
                break;
            case Key.Escape when _viewModel.IsSuggestionsOpen:
                _viewModel.IsSuggestionsOpen = false;
                e.Handled = true;
                break;
            case Key.Enter when _viewModel.SearchCommand.CanExecute(null):
                _viewModel.SearchCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private void OnSuggestionKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.AcceptSuggestionCommand.Execute(SuggestionList.SelectedItem);
        }
        else if (e.Key == Key.Escape)
        {
            _viewModel.IsSuggestionsOpen = false;
        }
        else
        {
            return;
        }

        SearchBox.Focus();
        e.Handled = true;
    }

    private void OnSuggestionClick(object sender, MouseButtonEventArgs e)
    {
        if (FindAncestor<ListBoxItem>(e.OriginalSource) is { DataContext: DirectoryUser user })
        {
            _viewModel.AcceptSuggestionCommand.Execute(user);
            e.Handled = true;
        }
    }

    private void OnGridDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Ignore double-clicks on headers and scroll bars.
        if (FindAncestor<DataGridRow>(e.OriginalSource) is not null)
        {
            SelectHighlightedUser();
        }
    }

    private void OnGridKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SelectHighlightedUser();
            e.Handled = true;
        }
    }

    private void SelectHighlightedUser()
    {
        if (_viewModel.SelectUserCommand.CanExecute(null))
        {
            _viewModel.SelectUserCommand.Execute(null);
        }
    }

    private static T? FindAncestor<T>(object source) where T : DependencyObject
    {
        var current = source as DependencyObject;
        while (current is not null and not T)
        {
            current = current is Visual ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
        }

        return current as T;
    }
}
