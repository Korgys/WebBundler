namespace WebBundler.Core;

public sealed record AssetOutput(
    string OutputPath,
    IReadOnlyList<string> SourceFiles,
    string? Fingerprint = null,
    long? Length = null,
    string? ContentHash = null);
