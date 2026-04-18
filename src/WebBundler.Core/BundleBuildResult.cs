namespace WebBundler.Core;

public sealed record BundleBuildResult(
    IReadOnlyList<AssetOutput> Outputs,
    IReadOnlyList<BuildMessage> Messages)
{
    public bool Succeeded => Messages.All(message => message.Severity != BuildSeverity.Error);
}
