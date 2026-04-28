using WebBundler.Core;

namespace WebBundler.Configuration;

public sealed class BundleConfigurationValidator
{
    public ConfigurationValidationResult Validate(BundleConfigurationDocument configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var messages = new List<BuildMessage>();

        if (configuration.Version != 1)
        {
            messages.Add(new BuildMessage(
                BuildSeverity.Error,
                $"Unsupported configuration version '{configuration.Version}'. Expected version 1."));
        }

        if (configuration.Bundles.Count == 0)
        {
            messages.Add(new BuildMessage(BuildSeverity.Error, "At least one bundle definition is required."));
            return new ConfigurationValidationResult(messages);
        }

        var outputs = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        for (var index = 0; index < configuration.Bundles.Count; index++)
        {
            var bundle = configuration.Bundles[index];
            var location = bundle.Output;
            var normalizedOutput = NormalizePath(bundle.Output);

            if (string.IsNullOrWhiteSpace(bundle.Output))
            {
                messages.Add(new BuildMessage(BuildSeverity.Error, $"Bundle #{index + 1} is missing an output path."));
            }

            if (!outputs.Add(normalizedOutput))
            {
                messages.Add(new BuildMessage(
                    BuildSeverity.Error,
                    $"Duplicate bundle output '{bundle.Output}' detected.",
                    Path: bundle.Output));
            }

            if (bundle.SourceMap == true && !string.IsNullOrWhiteSpace(bundle.Output))
            {
                var sourceMapOutput = NormalizePath(GetSourceMapOutputPath(bundle.Output));
                if (!outputs.Add(sourceMapOutput))
                {
                    messages.Add(new BuildMessage(
                        BuildSeverity.Error,
                        $"Bundle '{bundle.Output}' source map output '{sourceMapOutput}' conflicts with another output.",
                        Path: bundle.Output));
                }
            }

            if (bundle.Inputs is null || bundle.Inputs.Count == 0)
            {
                messages.Add(new BuildMessage(
                    BuildSeverity.Error,
                    $"Bundle '{bundle.Output}' must declare at least one input.",
                    Path: location));
            }

            if (!string.IsNullOrWhiteSpace(bundle.Output) && !IsExtensionCompatible(bundle.Output, bundle.Type))
            {
                messages.Add(new BuildMessage(
                    BuildSeverity.Warning,
                    $"Bundle '{bundle.Output}' does not use a conventional extension for '{bundle.Type}'."));
            }
        }

        if (configuration.ManifestOutput is not null)
        {
            if (string.IsNullOrWhiteSpace(configuration.ManifestOutput))
            {
                messages.Add(new BuildMessage(
                    BuildSeverity.Error,
                    "Manifest output path cannot be empty.",
                    Path: configuration.ManifestOutput));
            }
            else if (outputs.Contains(NormalizePath(configuration.ManifestOutput)))
            {
                messages.Add(new BuildMessage(
                    BuildSeverity.Error,
                    $"Manifest output '{configuration.ManifestOutput}' must not conflict with a bundle output.",
                    Path: configuration.ManifestOutput));
            }
        }

        return new ConfigurationValidationResult(messages);
    }

    private static bool IsExtensionCompatible(string output, BundleType type)
    {
        var extension = Path.GetExtension(output).ToLowerInvariant();
        return type switch
        {
            BundleType.Css => extension is ".css",
            BundleType.JavaScript => extension is ".js",
            _ => false
        };
    }

    private static string GetSourceMapOutputPath(string output) =>
        Path.Combine(Path.GetDirectoryName(output) ?? string.Empty, Path.GetFileName(output) + ".map");

    private static string NormalizePath(string path) => path.Replace('\\', '/').Trim();
}
