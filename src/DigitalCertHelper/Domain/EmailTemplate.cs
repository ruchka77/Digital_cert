using DigitalCertHelper.Models;

namespace DigitalCertHelper.Domain;

/// <summary>The approved renewal request. Deliberately compiled in so that it cannot be altered by configuration.</summary>
public static class EmailTemplate
{
    public const string Recipient = "supportgov@digital.gov.il";
    public const string Title = "הפקת תעודה דיגיטלית למחשב נייד לצורך חיבור מרחוק";

    public static EmailDraft Create(string displayName, string nationalId, CertificateFile certificate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(nationalId);
        ArgumentNullException.ThrowIfNull(certificate);

        var body = string.Join(
            "\r\n\r\n",
            Title,
            $"שם משתמש : {displayName}",
            $"תעודת זהות : {nationalId}");

        return new EmailDraft(Recipient, $"{Title} - {displayName}", body, certificate);
    }
}
