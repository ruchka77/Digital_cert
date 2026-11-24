using DigitalCertHelper.Application;
using DigitalCertHelper.Domain;
using Microsoft.Extensions.Logging;

namespace DigitalCertHelper.Services;

/// <summary>The single place where exceptions are logged and turned into messages that are safe to show.</summary>
public sealed class ErrorHandler(IDialogService dialogs, ILogger<ErrorHandler> logger) : IErrorHandler
{
    public string GetUserMessage(Exception exception) => exception switch
    {
        AppException app => app.UserMessage ?? DefaultMessage(app.Code),
        UnauthorizedAccessException => DefaultMessage(ErrorCode.AccessDenied),
        _ => DefaultMessage(ErrorCode.Unexpected),
    };

    public string Log(Exception exception, string operation)
    {
        logger.LogError(exception, "Operation failed: {Operation}", operation);
        return GetUserMessage(exception);
    }

    public void Handle(Exception exception, string operation) => dialogs.ShowError(Log(exception, operation));

    private static string DefaultMessage(ErrorCode code) => code switch
    {
        ErrorCode.DirectoryUnavailable =>
            "Active Directory could not be reached. Check that you are connected to the organization's network and try again.",
        ErrorCode.DirectoryAccessDenied =>
            "Access to Active Directory was denied. Sign in with your domain account, or contact your administrator.",
        ErrorCode.DirectoryQueryFailed =>
            "The Active Directory search could not be completed. Try again or contact your administrator.",
        ErrorCode.OutlookNotInstalled =>
            "Classic Microsoft Outlook is not installed on this computer.",
        ErrorCode.OutlookUnavailable =>
            "Outlook is not responding. Close any open Outlook dialogs, make sure Outlook and this application are not running as administrator, and try again.",
        ErrorCode.OutlookDraftFailed =>
            "The Outlook draft could not be created. Make sure an Outlook profile is configured and try again.",
        ErrorCode.CertificateMissing =>
            "The certificate file no longer exists. Select the certificate again.",
        ErrorCode.CertificateInvalid => "The certificate file is not valid.",
        ErrorCode.AccessDenied => "You do not have permission to perform this action.",
        _ => "An unexpected error occurred. Details were written to the log; contact your administrator.",
    };
}
