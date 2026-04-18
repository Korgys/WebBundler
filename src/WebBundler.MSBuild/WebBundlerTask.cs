using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using WebBundler.Configuration;
using WebBundler.Core;
using WebBundler.Fingerprinting;
using WebBundler.Minification;

namespace WebBundler.MSBuild;

public sealed class WebBundlerTask : Microsoft.Build.Utilities.Task
{
    [Required]
    public string ProjectDirectory { get; set; } = string.Empty;

    public string? ConfigurationFile { get; set; }

    public bool WriteOutputs { get; set; } = true;

    public bool EnableFingerprinting { get; set; }

    public override bool Execute()
    {
        var configurationPath = ConfigurationFile;
        if (string.IsNullOrWhiteSpace(configurationPath))
        {
            configurationPath = Path.Combine(ProjectDirectory, "bundleconfig.json");
        }

        var loader = new BundleConfigurationLoader();
        var loadResult = loader.Load(configurationPath);
        LogMessages(loadResult.Messages);
        if (!loadResult.Succeeded)
        {
            return false;
        }

        var validator = new BundleConfigurationValidator();
        var validationResult = validator.Validate(loadResult.Configuration!);
        LogMessages(validationResult.Messages);
        if (!validationResult.IsValid)
        {
            return false;
        }

        var fingerprinter = EnableFingerprinting ? new Sha256AssetFingerprinter() : null;
        var buildService = new BundleBuildService(DefaultAssetMinifiers.Create(), fingerprinter);
        var buildResult = buildService.Build(
            new BundleBuildRequest(
                new BuildContext(ProjectDirectory, configurationPath),
                loadResult.Configuration!.Bundles,
                WriteOutputs));

        LogMessages(buildResult.Messages);
        return buildResult.Succeeded;
    }

    private void LogMessages(IEnumerable<BuildMessage> messages)
    {
        foreach (var message in messages)
        {
            switch (message.Severity)
            {
                case BuildSeverity.Info:
                    Log.LogMessage(MessageImportance.High, message.Message);
                    break;
                case BuildSeverity.Warning:
                    Log.LogWarning(message.Message);
                    break;
                case BuildSeverity.Error:
                    Log.LogError(message.Message);
                    break;
            }
        }
    }
}
