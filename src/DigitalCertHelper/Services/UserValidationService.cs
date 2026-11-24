using DigitalCertHelper.Application;
using DigitalCertHelper.Models;
using Microsoft.Extensions.Logging;

namespace DigitalCertHelper.Services;

public sealed class UserValidationService(IDirectoryService directory, ILogger<UserValidationService> logger)
    : IUserValidationService
{
    public async Task<UserValidationResult> ValidateAsync(
        IReadOnlyCollection<DirectoryUser> selectedUsers, CancellationToken cancellationToken)
    {
        if (selectedUsers.Count != 1)
        {
            return Fail([selectedUsers.Count == 0 ? "No user selected." : "Select exactly one user."], null);
        }

        // Re-read the user: the search result may be out of date.
        var objectId = selectedUsers.Single().ObjectId;
        var user = await directory.FindByIdAsync(objectId, cancellationToken);
        if (user is null)
        {
            return Fail(["User not found in Active Directory."], objectId);
        }

        var errors = new List<string>();
        CheckText(user.DisplayName, "Display Name", errors);
        CheckText(user.UserLogonName, "User Logon Name", errors);
        if (user.IsEnabled is null)
        {
            errors.Add("The account status could not be verified.");
        }
        else if (user.IsEnabled == false)
        {
            errors.Add("Account is disabled.");
        }

        return errors.Count == 0 ? new UserValidationResult(user, []) : Fail(errors, objectId);
    }

    private static void CheckText(string value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{name} is missing.");
        }
        else if (value.Any(char.IsControl))
        {
            errors.Add($"{name} contains invalid characters.");
        }
    }

    private UserValidationResult Fail(IReadOnlyList<string> errors, Guid? objectId)
    {
        logger.LogWarning("User validation failed for {ObjectId}: {Errors}", objectId, errors);
        return new UserValidationResult(null, errors);
    }
}
