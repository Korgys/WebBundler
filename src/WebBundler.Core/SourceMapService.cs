using System.Text;
using System.Text.Json;
using System.Linq;

namespace WebBundler.Core;

internal static class SourceMapService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Create(
        string bundleOutputPath,
        string sourceMapOutputPath,
        IReadOnlyList<string> sourceFiles,
        IReadOnlyList<string> sourceContents,
        bool minify,
        IAssetMinifier? minifier)
    {
        if (sourceFiles.Count != sourceContents.Count)
        {
            throw new ArgumentException("Source files and source contents must have the same count.");
        }

        if (minify)
        {
            ArgumentNullException.ThrowIfNull(minifier);
        }

        var mapDirectory = Path.GetDirectoryName(sourceMapOutputPath) ?? string.Empty;
        var document = new SourceMapDocument
        {
            File = Path.GetFileName(bundleOutputPath),
            Sources = sourceFiles.Select(sourceFile => NormalizeRelativePath(mapDirectory, sourceFile)).ToArray(),
            SourcesContent = sourceContents.ToArray(),
            Mappings = BuildMappings(sourceContents, minify, minifier)
        };

        return JsonSerializer.Serialize(document, SerializerOptions);
    }

    private static string BuildMappings(IReadOnlyList<string> sourceContents, bool minify, IAssetMinifier? minifier)
    {
        var builder = new StringBuilder();
        var previousSourceIndex = 0;
        var previousOriginalLine = 0;
        var previousOriginalColumn = 0;

        if (minify)
        {
            var chunks = sourceContents.Select(content => minifier!.Minify(content.TrimEnd())).ToArray();
            var currentGeneratedColumn = 0;
            var previousGeneratedColumn = 0;

            for (var sourceIndex = 0; sourceIndex < sourceContents.Count; sourceIndex++)
            {
                var chunk = chunks[sourceIndex];
                if (chunk.Length == 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(',');
                }

                builder.Append(EncodeSegment(
                    currentGeneratedColumn - previousGeneratedColumn,
                    sourceIndex - previousSourceIndex,
                    -previousOriginalLine,
                    -previousOriginalColumn));

                previousSourceIndex = sourceIndex;
                previousOriginalLine = 0;
                previousOriginalColumn = 0;
                previousGeneratedColumn = currentGeneratedColumn;
                currentGeneratedColumn += chunk.Length;

                if (chunks[(sourceIndex + 1)..].Any(nextChunk => nextChunk.Length > 0))
                {
                    currentGeneratedColumn += 1;
                }
            }

            return builder.ToString();
        }

        for (var sourceIndex = 0; sourceIndex < sourceContents.Count; sourceIndex++)
        {
            var lines = SplitLines(sourceContents[sourceIndex].TrimEnd());
            for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                if (builder.Length > 0)
                {
                    builder.Append(';');
                }

                builder.Append(EncodeSegment(
                    0,
                    sourceIndex - previousSourceIndex,
                    lineIndex - previousOriginalLine,
                    -previousOriginalColumn));

                previousSourceIndex = sourceIndex;
                previousOriginalLine = lineIndex;
                previousOriginalColumn = 0;
            }
        }

        return builder.ToString();
    }

    private static IReadOnlyList<string> SplitLines(string content)
    {
        if (content.Length == 0)
        {
            return [string.Empty];
        }

        return content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
    }

    private static string NormalizeRelativePath(string baseDirectory, string path) =>
        Path.GetRelativePath(baseDirectory, path).Replace('\\', '/');

    private static string EncodeSegment(int generatedColumn, int sourceIndex, int originalLine, int originalColumn)
    {
        var builder = new StringBuilder();
        builder.Append(EncodeVlq(generatedColumn));
        builder.Append(EncodeVlq(sourceIndex));
        builder.Append(EncodeVlq(originalLine));
        builder.Append(EncodeVlq(originalColumn));
        return builder.ToString();
    }

    private static string EncodeVlq(int value)
    {
        var vlq = value < 0 ? ((-value) << 1) + 1 : value << 1;
        var builder = new StringBuilder();

        do
        {
            var digit = vlq & 31;
            vlq >>= 5;
            if (vlq > 0)
            {
                digit |= 32;
            }

            builder.Append(Base64Chars[digit]);
        }
        while (vlq > 0);

        return builder.ToString();
    }

    private const string Base64Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    private sealed class SourceMapDocument
    {
        public int Version { get; init; } = 3;

        public string File { get; init; } = string.Empty;

        public string[] Sources { get; init; } = [];

        public string[] SourcesContent { get; init; } = [];

        public string Mappings { get; init; } = string.Empty;
    }
}
