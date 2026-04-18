using WebBundler.Core;

namespace WebBundler.Configuration;

public sealed record ConfigurationValidationResult(IReadOnlyList<BuildMessage> Messages)
{
    public bool IsValid => Messages.All(message => message.Severity != BuildSeverity.Error);
}
