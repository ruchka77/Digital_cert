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
            return Fail([selectedUsers.Count == 0 ? "לא נבחר משתמש." : "יש לבחור משתמש אחד בלבד."], null);
        }

        // Re-read the user: the search result may be out of date.
        var objectId = selectedUsers.Single().ObjectId;
        var user = await directory.FindByIdAsync(objectId, cancellationToken);
        if (user is null)
        {
            return Fail(["המשתמש לא נמצא ב-Active Directory."], objectId);
        }

        var errors = new List<string>();
        CheckText(user.DisplayName, "שם התצוגה (Display Name)", errors);
        CheckText(user.UserLogonName, "שם הכניסה (User Logon Name)", errors);
        if (user.IsEnabled is null)
        {
            errors.Add("לא ניתן לאמת את מצב החשבון.");
        }
        else if (user.IsEnabled == false)
        {
            errors.Add("החשבון מושבת.");
        }

        return errors.Count == 0 ? new UserValidationResult(user, []) : Fail(errors, objectId);
    }

    private static void CheckText(string value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{name} חסר.");
        }
        else if (value.Any(char.IsControl))
        {
            errors.Add($"{name} מכיל תווים לא תקינים.");
        }
    }

    private UserValidationResult Fail(IReadOnlyList<string> errors, Guid? objectId)
    {
        logger.LogWarning("User validation failed for {ObjectId}: {Errors}", objectId, errors);
        return new UserValidationResult(null, errors);
    }
}
