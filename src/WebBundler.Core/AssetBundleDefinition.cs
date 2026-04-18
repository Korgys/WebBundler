using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebBundler.Core;

public sealed class AssetBundleDefinition
{
    public required string Output { get; init; }

    public required IReadOnlyList<string> Inputs { get; init; }

    public required BundleType Type { get; init; }

    public bool Minify { get; init; } = true;

    public bool? Fingerprint { get; init; }

    public bool? SourceMap { get; init; }

    public string? Environment { get; init; }

    public IReadOnlyList<string>? Include { get; init; }

    public IReadOnlyList<string>? Exclude { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
