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
            error = $"יש להזין לפחות {MinimumLength} תווים משם הפרטי או משם המשפחה של המשתמש.";
        }
        else if (term.Length > MaximumLength)
        {
            error = $"טקסט החיפוש לא יכול לעלות על {MaximumLength} תווים.";
        }
        else if (term.Any(char.IsControl) || !term.Any(char.IsLetterOrDigit))
        {
            error = "טקסט החיפוש מכיל תווים לא תקינים.";
        }

        return error is null;
    }
}
