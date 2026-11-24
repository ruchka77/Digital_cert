namespace DigitalCertHelper.Domain;

public enum ErrorCode
{
    Unexpected,
    DirectoryUnavailable,
    DirectoryAccessDenied,
    DirectoryQueryFailed,
    OutlookNotInstalled,
    OutlookUnavailable,
    OutlookDraftFailed,
    CertificateMissing,
    CertificateInvalid,
    AccessDenied,
}
