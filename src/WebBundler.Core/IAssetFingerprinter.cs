namespace WebBundler.Core;

public interface IAssetFingerprinter
{
    FingerprintResult Fingerprint(string outputPath, string content);
}
