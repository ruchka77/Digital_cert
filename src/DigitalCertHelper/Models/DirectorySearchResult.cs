namespace DigitalCertHelper.Models;

public sealed record DirectorySearchResult(IReadOnlyList<DirectoryUser> Users, bool IsTruncated);
