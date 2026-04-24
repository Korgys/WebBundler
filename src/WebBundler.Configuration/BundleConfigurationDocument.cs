using System.Text.Json;

namespace WebBundler.Configuration;

public sealed class BundleConfigurationDocument
{
    public int Version { get; init; } = 1;

    public string? ManifestOutput { get; init; }

    public required IReadOnlyList<WebBundler.Core.AssetBundleDefinition> Bundles { get; init; }

    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
