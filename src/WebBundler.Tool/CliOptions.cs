namespace WebBundler.Tool;

public sealed record CliOptions(
    CliCommand Command,
    string? ConfigurationPath,
    bool ShowHelp);

public enum CliCommand
{
    Build,
    Validate,
    Help
}
