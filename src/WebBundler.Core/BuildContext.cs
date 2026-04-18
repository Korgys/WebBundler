namespace WebBundler.Core;

public sealed record BuildContext(
    string RootDirectory,
    string? ConfigurationPath = null);
