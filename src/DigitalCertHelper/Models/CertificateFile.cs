namespace DigitalCertHelper.Models;

/// <param name="FullPath">Absolute path of the selected file.</param>
/// <param name="MayContainPrivateKey">True when sending the file could disclose a private key.</param>
public sealed record CertificateFile(string FullPath, bool MayContainPrivateKey)
{
    public string FileName => Path.GetFileName(FullPath);
}
