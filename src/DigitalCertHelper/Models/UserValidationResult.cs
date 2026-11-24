namespace DigitalCertHelper.Models;

/// <summary><see cref="User"/> holds data freshly read from Active Directory, and is set only when validation passed.</summary>
public sealed record UserValidationResult(DirectoryUser? User, IReadOnlyList<string> Errors)
{
    public bool IsValid => User is not null && Errors.Count == 0;
}
