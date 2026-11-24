using System.Security;
using System.Text;
using DigitalCertHelper.Application;
using DigitalCertHelper.Domain;
using DigitalCertHelper.Models;

namespace DigitalCertHelper.Services;

/// <summary>Checks the selected file. Any file type is accepted; only existence, access and size are checked.</summary>
public sealed class CertificateService(CertificateOptions options) : ICertificateService
{
    private const int KeyScanBytes = 64 * 1024;
    private static readonly string[] KeyContainerExtensions = [".pfx", ".p12"];

    public string FileDialogFilter => "כל הקבצים (*.*)|*.*";

    public CertificateFile Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new AppException(ErrorCode.CertificateInvalid, "לא נבחר קובץ.");
        }

        try
        {
            var file = new FileInfo(Path.GetFullPath(path));
            if (!file.Exists)
            {
                throw new AppException(ErrorCode.CertificateMissing);
            }

            if (file.Length == 0)
            {
                throw new AppException(ErrorCode.CertificateInvalid, "הקובץ שנבחר ריק.");
            }

            if (file.Length > options.MaxFileSizeMegabytes * 1024L * 1024L)
            {
                throw new AppException(
                    ErrorCode.CertificateInvalid,
                    $"הקובץ שנבחר גדול מ-{options.MaxFileSizeMegabytes} MB.");
            }

            // Reading the start of the file also proves that the current user can access it.
            var buffer = new byte[Math.Min(file.Length, KeyScanBytes)];
            using (var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                stream.ReadExactly(buffer);
            }

            var mayContainKey =
                KeyContainerExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase)
                || Encoding.ASCII.GetString(buffer).Contains("PRIVATE KEY-----", StringComparison.Ordinal);
            return new CertificateFile(file.FullName, mayContainKey);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException)
        {
            throw new AppException(ErrorCode.AccessDenied, "אין לך הרשאה לקרוא את הקובץ שנבחר.", ex);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new AppException(ErrorCode.CertificateMissing, innerException: ex);
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or NotSupportedException)
        {
            throw new AppException(
                ErrorCode.CertificateInvalid,
                "לא ניתן לקרוא את הקובץ. ייתכן שהוא בשימוש או נמצא בכונן שנותק.",
                ex);
        }
    }
}
