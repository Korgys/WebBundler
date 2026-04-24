using System.Text.Json;

namespace WebBundler.Core;

public sealed class AssetManifestService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public AssetManifest Create(string rootDirectory, IReadOnlyList<AssetOutput> outputs)
    {
        ArgumentNullException.ThrowIfNull(rootDirectory);
        ArgumentNullException.ThrowIfNull(outputs);

        var bundles = new SortedDictionary<string, AssetManifestEntry>(StringComparer.Ordinal);

        foreach (var output in outputs.OrderBy(output => GetRelativeManifestPath(rootDirectory, GetLogicalOutputPath(output)), StringComparer.Ordinal))
        {
            var logicalOutputPath = GetRelativeManifestPath(rootDirectory, GetLogicalOutputPath(output));
            var generatedOutputPath = NormalizeManifestPath(Path.GetRelativePath(rootDirectory, output.OutputPath));
            var bundleType = GetBundleType(output);

            bundles[logicalOutputPath] = new AssetManifestEntry(
                generatedOutputPath,
                output.Fingerprint is not null,
                bundleType);
        }

        return new AssetManifest(1, bundles);
    }

    public string Serialize(AssetManifest manifest) =>
        JsonSerializer.Serialize(manifest, SerializerOptions);

    private static string GetLogicalOutputPath(AssetOutput output) =>
        string.IsNullOrWhiteSpace(output.LogicalOutputPath)
            ? throw new InvalidOperationException("Asset output is missing its logical output path.")
            : output.LogicalOutputPath;

    private static string GetBundleType(AssetOutput output) =>
        output.Type switch
        {
            BundleType.Css => "css",
            BundleType.JavaScript => "js",
            null => throw new InvalidOperationException("Asset output is missing its bundle type."),
            _ => throw new InvalidOperationException($"Unsupported bundle type '{output.Type}'.")
        };

    private static string GetRelativeManifestPath(string rootDirectory, string path) =>
        NormalizeManifestPath(Path.GetRelativePath(rootDirectory, Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(rootDirectory, path))));

    private static string NormalizeManifestPath(string path) =>
        path.Replace('\\', '/').Trim();
}
