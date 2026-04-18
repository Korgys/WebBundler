using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace WebBundler.Core;

public sealed class BundleBuildService
{
    private readonly IAssetFileSystem fileSystem;
    private readonly IReadOnlyDictionary<BundleType, IAssetMinifier> minifiers;
    private readonly IAssetFingerprinter? fingerprinter;

    public BundleBuildService(
        IEnumerable<IAssetMinifier> minifiers,
        IAssetFingerprinter? fingerprinter = null,
        IAssetFileSystem? fileSystem = null)
    {
        this.fileSystem = fileSystem ?? new PhysicalAssetFileSystem();
        this.minifiers = minifiers.ToDictionary(minifier => minifier.SupportedType);
        this.fingerprinter = fingerprinter;
    }

    public BundleBuildResult Build(BundleBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Context);

        var outputs = new List<AssetOutput>();
        var messages = new List<BuildMessage>();
        var seenOutputs = new HashSet<string>(GetPathComparer());
        var rootDirectory = NormalizeDirectory(request.Context.RootDirectory);

        foreach (var bundle in request.Bundles)
        {
            if (string.IsNullOrWhiteSpace(bundle.Output))
            {
                messages.Add(new BuildMessage(BuildSeverity.Error, "Bundle output path is required."));
                continue;
            }

            if (bundle.Inputs.Count == 0)
            {
                messages.Add(new BuildMessage(
                    BuildSeverity.Error,
                    $"Bundle '{bundle.Output}' must define at least one input.",
                    Path: bundle.Output));
                continue;
            }

            var resolvedInputs = ResolveInputs(rootDirectory, bundle.Inputs, messages, bundle.Output, out var inputsResolved);
            if (!inputsResolved || resolvedInputs.Count == 0)
            {
                continue;
            }

            var outputPath = ResolvePath(rootDirectory, bundle.Output);
            if (!seenOutputs.Add(NormalizePath(outputPath)))
            {
                messages.Add(new BuildMessage(
                    BuildSeverity.Error,
                    $"Multiple bundles resolve to the same output path '{bundle.Output}'.",
                    Path: bundle.Output));
                continue;
            }

            var content = ComposeContent(resolvedInputs.Select(path => fileSystem.ReadAllText(path)));

            if (bundle.Minify)
            {
                if (!minifiers.TryGetValue(bundle.Type, out var minifier))
                {
                    messages.Add(new BuildMessage(
                        BuildSeverity.Error,
                        $"No minifier is registered for bundle type '{bundle.Type}'.",
                        Path: bundle.Output));
                    continue;
                }

                content = minifier.Minify(content);
            }

            var finalOutputPath = outputPath;
            var fingerprint = default(FingerprintResult);
            if (bundle.Fingerprint == true)
            {
                if (fingerprinter is null)
                {
                    messages.Add(new BuildMessage(
                        BuildSeverity.Warning,
                        $"Bundle '{bundle.Output}' requested fingerprinting, but no fingerprinter is configured.",
                        Path: bundle.Output));
                }
                else
                {
                    fingerprint = fingerprinter.Fingerprint(outputPath, content);
                    finalOutputPath = ResolvePath(rootDirectory, fingerprint.FingerprintedPath);
                }
            }

            if (request.WriteOutputs)
            {
                fileSystem.CreateDirectory(fileSystem.GetDirectoryName(finalOutputPath));
                fileSystem.WriteAllText(finalOutputPath, content);
            }

            outputs.Add(new AssetOutput(
                finalOutputPath,
                resolvedInputs,
                fingerprint?.Value,
                Encoding.UTF8.GetByteCount(content),
                ComputeHash(content)));

            messages.Add(new BuildMessage(
                BuildSeverity.Info,
                $"Built '{bundle.Output}' from {resolvedInputs.Count} file(s).",
                Path: bundle.Output));
        }

        return new BundleBuildResult(outputs, messages);
    }

    private List<string> ResolveInputs(
        string rootDirectory,
        IReadOnlyList<string> patterns,
        ICollection<BuildMessage> messages,
        string bundleOutput,
        out bool success)
    {
        var resolved = new List<string>();
        var seen = new HashSet<string>(GetPathComparer());
        success = true;

        foreach (var pattern in patterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                success = false;
                messages.Add(new BuildMessage(
                    BuildSeverity.Error,
                    $"Bundle '{bundleOutput}' contains an empty input pattern.",
                    Path: bundleOutput));
                continue;
            }

            var matches = ResolvePattern(rootDirectory, pattern).ToList();
            if (matches.Count == 0)
            {
                success = false;
                messages.Add(new BuildMessage(
                    BuildSeverity.Error,
                    $"Input pattern '{pattern}' did not match any files.",
                    Path: pattern));
                continue;
            }

            foreach (var match in matches)
            {
                if (seen.Add(NormalizePath(match)))
                {
                    resolved.Add(match);
                }
            }
        }

        return resolved;
    }

    private IEnumerable<string> ResolvePattern(string rootDirectory, string pattern)
    {
        var normalizedPattern = NormalizePattern(pattern);
        var absolutePattern = ResolvePath(rootDirectory, normalizedPattern);

        if (!ContainsGlob(absolutePattern))
        {
            if (fileSystem.FileExists(absolutePattern))
            {
                yield return absolutePattern;
            }

            yield break;
        }

        var searchRoot = GetSearchRoot(rootDirectory, normalizedPattern);
        if (!fileSystem.DirectoryExists(searchRoot))
        {
            yield break;
        }

        var regex = new Regex(
            "^" + GlobToRegex(Path.GetRelativePath(rootDirectory, absolutePattern)) + "$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        foreach (var candidate in fileSystem.EnumerateFiles(searchRoot, recursive: true)
                     .OrderBy(path => NormalizePath(path), StringComparer.Ordinal))
        {
            var relative = NormalizePattern(Path.GetRelativePath(rootDirectory, candidate));
            if (regex.IsMatch(relative))
            {
                yield return candidate;
            }
        }
    }

    private static string ComposeContent(IEnumerable<string> segments) =>
        string.Join("\n", segments.Select(segment => segment.TrimEnd()));

    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static bool ContainsGlob(string path) =>
        path.Contains('*') || path.Contains('?') || path.Contains('[');

    private static string NormalizePattern(string pattern) =>
        pattern.Replace('\\', '/').Trim();

    private static string NormalizeDirectory(string directory) =>
        Path.GetFullPath(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    private static string ResolvePath(string rootDirectory, string path) =>
        Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(rootDirectory, path));

    private static string NormalizePath(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static IEqualityComparer<string> GetPathComparer() =>
        OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    private static string GetSearchRoot(string rootDirectory, string pattern)
    {
        var wildcardIndex = pattern.IndexOfAny(['*', '?', '[']);
        if (wildcardIndex < 0)
        {
            return NormalizeDirectory(rootDirectory);
        }

        var prefix = pattern[..wildcardIndex];
        var lastSlash = prefix.LastIndexOf('/');
        var root = lastSlash >= 0 ? prefix[..lastSlash] : string.Empty;
        return string.IsNullOrEmpty(root)
            ? NormalizeDirectory(rootDirectory)
            : ResolvePath(rootDirectory, root);
    }

    private static string GlobToRegex(string pattern)
    {
        var escaped = Regex.Escape(pattern.Replace('\\', '/'));
        escaped = escaped.Replace(@"\*\*/", "(.*/)?");
        escaped = escaped.Replace(@"\*\*", ".*");
        escaped = escaped.Replace(@"\*", "[^/]*");
        escaped = escaped.Replace(@"\?", "[^/]");
        return escaped;
    }
}
