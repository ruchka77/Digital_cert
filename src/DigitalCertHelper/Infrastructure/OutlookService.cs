using System.Runtime.InteropServices;
using DigitalCertHelper.Application;
using DigitalCertHelper.Domain;
using DigitalCertHelper.Helpers;
using DigitalCertHelper.Models;
using Microsoft.Extensions.Logging;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace DigitalCertHelper.Infrastructure;

/// <summary>
/// Creates and displays Outlook drafts. This class must never send mail; a unit test verifies that the
/// compiled assembly contains no reference to the Outlook send operation.
/// </summary>
public sealed class OutlookService(ILogger<OutlookService> logger) : IOutlookService
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(500);

    public Task CreateDraftAsync(EmailDraft draft) => StaTask.Run(() => CreateDraft(draft));

    private void CreateDraft(EmailDraft draft)
    {
        if (Type.GetTypeFromProgID("Outlook.Application") is null)
        {
            throw new AppException(ErrorCode.OutlookNotInstalled);
        }

        Outlook.Application? application = null;
        Outlook.MailItem? mail = null;
        Outlook.Attachments? attachments = null;
        Outlook.Attachment? attachment = null;
        var displayed = false;
        try
        {
            // Attaches to the running instance, or starts Outlook.
            application = Retry(() => new Outlook.Application());
            mail = Retry(() => (Outlook.MailItem)application.CreateItem(Outlook.OlItemType.olMailItem));

            RetryAction(() => mail.To = draft.Recipient);
            RetryAction(() => mail.Subject = draft.Subject);
            RetryAction(() => mail.BodyFormat = Outlook.OlBodyFormat.olFormatHTML);
            RetryAction(() => mail.HTMLBody = EmailHtmlFormatter.ToRightToLeftHtml(draft.Body));
            attachments = Retry(() => mail.Attachments);
            attachment = Retry(() => attachments.Add(
                draft.Attachment.FullPath, Outlook.OlAttachmentType.olByValue));

            RetryAction(() => mail.Display(false));
            displayed = true;
            SaveToDrafts(mail);
            logger.LogInformation("Outlook draft created and displayed");
        }
        catch (COMException ex)
        {
            throw new AppException(MapError(ex.HResult), innerException: ex);
        }
        catch (InvalidCastException ex)
        {
            // Damaged Office registration.
            throw new AppException(ErrorCode.OutlookUnavailable, innerException: ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new AppException(ErrorCode.AccessDenied, innerException: ex);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new AppException(ErrorCode.CertificateMissing, innerException: ex);
        }
        finally
        {
            if (!displayed && mail is not null)
            {
                Discard(mail);
            }

            Release(attachment);
            Release(attachments);
            Release(mail);
            Release(application); // Never Quit(): the draft belongs to the user now.
        }
    }

    private void SaveToDrafts(Outlook.MailItem mail)
    {
        try
        {
            mail.Save();
        }
        catch (Exception ex) when (ex is COMException or UnauthorizedAccessException)
        {
            // The compose window is open and usable, so this is not a failure for the user.
            logger.LogWarning(ex, "The displayed message could not be saved to the Drafts folder");
        }
    }

    private void Discard(Outlook.MailItem mail)
    {
        try
        {
            mail.Close(Outlook.OlInspectorClose.olDiscard);
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException)
        {
            logger.LogWarning(ex, "An incomplete message could not be discarded");
        }
    }

    private static void RetryAction(Action action) => Retry(() =>
    {
        action();
        return true;
    });

    /// <summary>Outlook rejects calls while it is busy, for example during start-up or with a dialog open.</summary>
    private static T Retry<T>(Func<T> action)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return action();
            }
            catch (COMException ex) when (attempt < MaxAttempts && IsBusy(ex.HResult))
            {
                Thread.Sleep(RetryDelay);
            }
        }
    }

    private static bool IsBusy(int hresult) => unchecked((uint)hresult) is 0x80010001 or 0x8001010A;

    internal static ErrorCode MapError(int hresult) => unchecked((uint)hresult) switch
    {
        0x80040154 => ErrorCode.OutlookNotInstalled, // class not registered
        0x80080005 // server execution failed, typically an elevation mismatch
            or 0x80010001 // call rejected
            or 0x8001010A // retry later
            or 0x800706BA // RPC server unavailable
            or 0x800706BE // RPC call failed
            or 0x80010108 // object disconnected
            or 0x800401E3 => ErrorCode.OutlookUnavailable,
        0x80070005 => ErrorCode.AccessDenied,
        0x80070002 or 0x80070003 => ErrorCode.CertificateMissing,
        _ => ErrorCode.OutlookDraftFailed,
    };

    private static void Release(object? comObject)
    {
        if (comObject is not null && Marshal.IsComObject(comObject))
        {
            Marshal.ReleaseComObject(comObject);
        }
    }
}
