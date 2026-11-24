namespace DigitalCertHelper.Models;

/// <summary>An Active Directory user. <see cref="IsEnabled"/> is null when the account state could not be read.</summary>
public sealed record DirectoryUser(
    Guid ObjectId,
    string DisplayName,
    string NationalId, // extensionAttribute1: the national ID (תעודת זהות)
    string Department,
    bool? IsEnabled)
{
    /// <summary>National IDs are personal data, so the default record text must not leak them to logs.</summary>
    public override string ToString() => $"User {ObjectId}";
}
