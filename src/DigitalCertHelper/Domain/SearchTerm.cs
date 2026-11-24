using System.Diagnostics.CodeAnalysis;

namespace DigitalCertHelper.Domain;

/// <summary>Validates user-search input.</summary>
public static class SearchTerm
{
    public const int MinimumLength = 2; // some Hebrew names have only two letters
    public const int SuggestionMinimumLength = 3;
    public const int MaximumLength = 64;

    public static bool TryNormalize(
        string? input,
        out string term,
        [NotNullWhen(false)] out string? error)
    {
        term = (input ?? string.Empty).Trim();
        error = null;

        if (term.Length < MinimumLength)
        {
            error = $"Enter at least {MinimumLength} characters of the user's first or last name.";
        }
        else if (term.Length > MaximumLength)
        {
            error = $"The search text cannot exceed {MaximumLength} characters.";
        }
        else if (term.Any(char.IsControl) || !term.Any(char.IsLetterOrDigit))
        {
            error = "The search text contains invalid characters.";
        }

        return error is null;
    }
}
