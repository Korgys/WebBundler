namespace WebBundler.Core;

public sealed record FingerprintResult(
    string OriginalPath,
    string FingerprintedPath,
    string Value);
