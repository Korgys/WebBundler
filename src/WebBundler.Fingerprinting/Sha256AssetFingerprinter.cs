using System.Security.Cryptography;
using System.Text;
using WebBundler.Core;

namespace WebBundler.Fingerprinting;

public sealed class Sha256AssetFingerprinter : IAssetFingerprinter
{
    public FingerprintResult Fingerprint(string outputPath, string content)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        var fingerprint = Convert.ToHexString(hash).ToLowerInvariant()[..8];
        var fingerprintedPath = InsertFingerprint(outputPath, fingerprint);
        return new FingerprintResult(outputPath, fingerprintedPath, fingerprint);
    }

    private static string InsertFingerprint(string path, string fingerprint)
    {
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        var combined = $"{fileName}.{fingerprint}{extension}";
        return string.IsNullOrEmpty(directory)
            ? combined
            : Path.Combine(directory, combined);
    }
}
