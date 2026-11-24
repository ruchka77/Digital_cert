namespace DigitalCertHelper.Models;

public sealed record EmailDraft(string Recipient, string Subject, string Body, CertificateFile Attachment)
{
    /// <summary>The content is personal data and must not leak to logs.</summary>
    public override string ToString() => nameof(EmailDraft);
}
