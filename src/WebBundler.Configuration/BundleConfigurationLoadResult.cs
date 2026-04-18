using WebBundler.Core;

namespace WebBundler.Configuration;

public sealed record BundleConfigurationLoadResult(
    BundleConfigurationDocument? Configuration,
    IReadOnlyList<BuildMessage> Messages)
{
    public bool Succeeded => Configuration is not null && Messages.All(message => message.Severity != BuildSeverity.Error);
}
