namespace WebBundler.Core;

public sealed record BuildMessage(
    BuildSeverity Severity,
    string Message,
    string? Code = null,
    string? Path = null);
