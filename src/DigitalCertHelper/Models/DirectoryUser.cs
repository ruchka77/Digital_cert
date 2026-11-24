namespace DigitalCertHelper.Models;

/// <summary>An Active Directory user. <see cref="IsEnabled"/> is null when the account state could not be read.</summary>
public sealed record DirectoryUser(
    Guid ObjectId,
    string DisplayName,
    string UserLogonName,
    string Department,
    bool? IsEnabled)
{
    /// <summary>Logon names are ID numbers, so the default record text must not leak them to logs.</summary>
    public override string ToString() => $"User {ObjectId}";
}
