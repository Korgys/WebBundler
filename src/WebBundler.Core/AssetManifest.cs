namespace WebBundler.Core;

public sealed record AssetManifest(
    int Version,
    IReadOnlyDictionary<string, AssetManifestEntry> Bundles);

public sealed record AssetManifestEntry(
    string Output,
    bool Fingerprinted,
    string Type);
