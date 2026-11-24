using System.DirectoryServices;
using System.Runtime.InteropServices;
using DigitalCertHelper.Application;
using DigitalCertHelper.Domain;
using DigitalCertHelper.Helpers;
using DigitalCertHelper.Models;
using Microsoft.Extensions.Logging;

namespace DigitalCertHelper.Infrastructure;

/// <summary>Queries Active Directory in the context of the signed-in Windows user.</summary>
public sealed class ActiveDirectoryService(DirectoryOptions options, ILogger<ActiveDirectoryService> logger)
    : IDirectoryService
{
    private const int AccountDisabledFlag = 0x2;

    private static readonly string[] Attributes =
        ["objectGUID", "displayName", "extensionAttribute1", "department", "userAccountControl"];

    public async Task<DirectorySearchResult> SearchAsync(
        string term, int maxResults, CancellationToken cancellationToken)
    {
        // Ask for one extra entry, to know whether more matches exist.
        var users = await QueryAsync(LdapFilterBuilder.BuildNameSearch(term), maxResults + 1, cancellationToken);

        // Search text is personal data and is therefore not logged.
        logger.LogInformation(
            "AD search completed: term length {TermLength}, {Count} match(es), limit {Limit}",
            term.Length, users.Count, maxResults);

        return new DirectorySearchResult(
            users.Take(maxResults)
                .OrderBy(u => u.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList(),
            users.Count > maxResults);
    }

    public async Task<DirectoryUser?> FindByIdAsync(Guid objectId, CancellationToken cancellationToken)
    {
        var users = await QueryAsync(LdapFilterBuilder.BuildObjectIdLookup(objectId), 1, cancellationToken);
        logger.LogInformation("AD lookup of {ObjectId}: found = {Found}", objectId, users.Count == 1);
        return users.SingleOrDefault();
    }

    // ADSI calls are synchronous and cannot be aborted. On cancellation the caller is
    // released immediately and the orphaned query ends on its own timeout.
    private Task<List<DirectoryUser>> QueryAsync(string filter, int limit, CancellationToken cancellationToken) =>
        Task.Run(() => Query(filter, limit, cancellationToken), cancellationToken).WaitAsync(cancellationToken);

    private List<DirectoryUser> Query(string filter, int limit, CancellationToken cancellationToken)
    {
        try
        {
            var timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

            // Null credentials = current Windows user. Traffic is signed and encrypted.
            using var root = new DirectoryEntry(
                $"LDAP://{options.Domain}", null, null,
                AuthenticationTypes.Secure | AuthenticationTypes.Signing | AuthenticationTypes.Sealing);
            using var searcher = new DirectorySearcher(root, filter, Attributes, SearchScope.Subtree)
            {
                SizeLimit = limit,
                ClientTimeout = timeout,
                ServerTimeLimit = timeout,
                ReferralChasing = ReferralChasingOption.None,
                CacheResults = false,
            };
            using var results = searcher.FindAll();

            var users = new List<DirectoryUser>();
            foreach (SearchResult result in results)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (Map(result) is { } user)
                {
                    users.Add(user);
                }
            }

            return users;
        }
        catch (COMException ex)
        {
            throw new AppException(MapError(ex.HResult), innerException: ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new AppException(ErrorCode.DirectoryAccessDenied, innerException: ex);
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or ArgumentException)
        {
            throw new AppException(ErrorCode.DirectoryQueryFailed, innerException: ex);
        }
    }

    internal static ErrorCode MapError(int hresult) => unchecked((uint)hresult) switch
    {
        0x80070005 // access denied
            or 0x8007052E // logon failure
            or 0x80072020 // operations error: not authenticated
            or 0x80072027 // authentication method not supported
            or 0x80072028 // strong authentication required
            or 0x80072030 // no such object, also returned when not permitted to see it
            or 0x80072098 => ErrorCode.DirectoryAccessDenied, // insufficient rights
        0x8007203A // server down
            or 0x8007054B // domain does not exist or cannot be contacted
            or 0x8007051F // no logon servers
            or 0x800705B4 // timeout
            or 0x8007200E // busy
            or 0x8007200F // unavailable
            or 0x80072022 // time limit exceeded
            or 0x80070035 // network path not found
            or 0x80005000 => ErrorCode.DirectoryUnavailable, // invalid ADs path: the domain cannot be resolved
        _ => ErrorCode.DirectoryQueryFailed,
    };

    private static DirectoryUser? Map(SearchResult result)
    {
        if (GetValue(result, "objectGUID") is not byte[] { Length: 16 } objectId)
        {
            return null;
        }

        bool? isEnabled = GetValue(result, "userAccountControl") is int flags
            ? (flags & AccountDisabledFlag) == 0
            : null;

        return new DirectoryUser(
            new Guid(objectId),
            GetText(result, "displayName"),
            GetText(result, "extensionAttribute1"), // the national ID; sAMAccountName is not used
            GetText(result, "department"),
            isEnabled);
    }

    private static object? GetValue(SearchResult result, string attribute) =>
        result.Properties[attribute] is { Count: > 0 } values ? values[0] : null;

    private static string GetText(SearchResult result, string attribute) =>
        (GetValue(result, attribute) as string)?.Trim() ?? string.Empty;
}
