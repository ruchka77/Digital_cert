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
            "לא ניתן להתחבר ל-Active Directory. ודא שהמחשב מחובר לרשת הארגונית ונסה שוב.",
        ErrorCode.DirectoryAccessDenied =>
            "הגישה ל-Active Directory נדחתה. התחבר עם חשבון הדומיין שלך או פנה למנהל המערכת.",
        ErrorCode.DirectoryQueryFailed =>
            "החיפוש ב-Active Directory לא הושלם. נסה שוב או פנה למנהל המערכת.",
        ErrorCode.OutlookNotInstalled =>
            "Microsoft Outlook (הגרסה הקלאסית) אינו מותקן במחשב זה.",
        ErrorCode.OutlookUnavailable =>
            "Outlook אינו מגיב. סגור חלונות דו-שיח פתוחים ב-Outlook, ודא ש-Outlook והיישום אינם פועלים כמנהל, ונסה שוב.",
        ErrorCode.OutlookDraftFailed =>
            "לא ניתן ליצור את הודעת הדואר ב-Outlook. ודא שהוגדר פרופיל Outlook ונסה שוב.",
        ErrorCode.CertificateMissing =>
            "הקובץ שנבחר אינו קיים עוד. בחר את הקובץ מחדש.",
        ErrorCode.CertificateInvalid => "הקובץ שנבחר אינו תקין.",
        ErrorCode.AccessDenied => "אין לך הרשאה לבצע פעולה זו.",
        _ => "אירעה שגיאה לא צפויה. הפרטים נרשמו ביומן; פנה למנהל המערכת.",
    };
}
