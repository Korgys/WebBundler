namespace WebBundler.Core;

public sealed record BundleBuildRequest(
    BuildContext Context,
    IReadOnlyList<AssetBundleDefinition> Bundles,
    bool WriteOutputs = true);
