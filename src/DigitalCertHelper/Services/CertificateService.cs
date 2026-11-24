using System.Security;
using System.Text;
using DigitalCertHelper.Application;
using DigitalCertHelper.Domain;
using DigitalCertHelper.Models;

namespace DigitalCertHelper.Services;

public sealed class CertificateService(CertificateOptions options) : ICertificateService
{
    private static readonly string[] KeyContainerExtensions = [".pfx", ".p12"];

    public string FileDialogFilter =>
        "Certificate files|" + string.Join(';', options.AllowedExtensions.Select(e => "*" + e));

    public CertificateFile Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new AppException(ErrorCode.CertificateInvalid, "No certificate file was selected.");
        }

        try
        {
            var file = new FileInfo(Path.GetFullPath(path));
            if (!file.Exists)
            {
                throw new AppException(ErrorCode.CertificateMissing);
            }

            if (!options.AllowedExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase))
            {
                throw new AppException(
                    ErrorCode.CertificateInvalid,
                    "The selected file type is not supported. Supported types: "
                        + string.Join(", ", options.AllowedExtensions));
            }

            if (file.Length == 0)
            {
                throw new AppException(ErrorCode.CertificateInvalid, "The selected certificate file is empty.");
            }

            if (file.Length > options.MaxFileSizeMegabytes * 1024L * 1024L)
            {
                throw new AppException(
                    ErrorCode.CertificateInvalid,
                    $"The selected file is larger than {options.MaxFileSizeMegabytes} MB.");
            }

            // Reading also proves that the current user can access the file.
            var content = Encoding.ASCII.GetString(File.ReadAllBytes(file.FullName));
            var mayContainKey =
                KeyContainerExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase)
                || content.Contains("PRIVATE KEY-----", StringComparison.Ordinal);
            return new CertificateFile(file.FullName, mayContainKey);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException)
        {
            throw new AppException(
                ErrorCode.AccessDenied, "You do not have permission to read the selected certificate file.", ex);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new AppException(ErrorCode.CertificateMissing, innerException: ex);
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or NotSupportedException)
        {
            throw new AppException(
                ErrorCode.CertificateInvalid,
                "The certificate file could not be read. It may be in use, or on a disconnected drive.",
                ex);
        }
    }
}
