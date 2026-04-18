using WebBundler.Configuration;
using WebBundler.Core;
using WebBundler.Fingerprinting;
using WebBundler.Minification;

namespace WebBundler.Tool;

public sealed class CliApplication
{
    public int Run(string[] args, TextWriter? stdout = null, TextWriter? stderr = null)
    {
        stdout ??= Console.Out;
        stderr ??= Console.Error;

        if (!TryParse(args, out var options, out var parseError))
        {
            if (!string.IsNullOrWhiteSpace(parseError))
            {
                stderr.WriteLine(parseError);
            }

            WriteHelp(stdout);
            return 1;
        }

        if (options.ShowHelp || options.Command == CliCommand.Help)
        {
            WriteHelp(stdout);
            return 0;
        }

        var configurationPath = ResolveConfigurationPath(options.ConfigurationPath);
        var loader = new BundleConfigurationLoader();
        var loadResult = loader.Load(configurationPath);
        WriteMessages(loadResult.Messages, stdout, stderr);
        if (!loadResult.Succeeded)
        {
            return 2;
        }

        var validator = new BundleConfigurationValidator();
        var validationResult = validator.Validate(loadResult.Configuration!);
        WriteMessages(validationResult.Messages, stdout, stderr);
        if (!validationResult.IsValid)
        {
            return 2;
        }

        var fingerprinter = new Sha256AssetFingerprinter();
        var buildService = new BundleBuildService(DefaultAssetMinifiers.Create(), fingerprinter);
        var buildResult = buildService.Build(
            new BundleBuildRequest(
                new BuildContext(
                    Path.GetDirectoryName(Path.GetFullPath(configurationPath)) ?? Directory.GetCurrentDirectory(),
                    configurationPath),
                loadResult.Configuration!.Bundles,
                WriteOutputs: options.Command == CliCommand.Build));

        WriteMessages(buildResult.Messages, stdout, stderr);
        return buildResult.Succeeded ? 0 : 3;
    }

    private static bool TryParse(string[] args, out CliOptions options, out string? parseError)
    {
        options = new CliOptions(CliCommand.Help, null, ShowHelp: true);
        parseError = null;

        if (args.Length == 0)
        {
            return true;
        }

        var command = args[0].ToLowerInvariant();
        if (command is "-h" or "--help" or "/?")
        {
            return true;
        }

        if (command is not ("build" or "validate" or "help"))
        {
            parseError = $"Unknown command '{args[0]}'.";
            return false;
        }

        string? configurationPath = null;
        for (var index = 1; index < args.Length; index++)
        {
            var arg = args[index];
            if (arg is "-h" or "--help")
            {
                options = new CliOptions(ParseCommand(command), configurationPath, ShowHelp: true);
                return true;
            }

            if (arg is "--config" or "-c")
            {
                if (index + 1 >= args.Length)
                {
                    parseError = "Missing value for --config.";
                    return false;
                }

                configurationPath = args[++index];
                continue;
            }

            parseError = $"Unknown argument '{arg}'.";
            return false;
        }

        options = new CliOptions(ParseCommand(command), configurationPath, ShowHelp: false);
        return true;
    }

    private static CliCommand ParseCommand(string command) =>
        command switch
        {
            "build" => CliCommand.Build,
            "validate" => CliCommand.Validate,
            _ => CliCommand.Help
        };

    private static string ResolveConfigurationPath(string? configurationPath) =>
        Path.GetFullPath(configurationPath ?? "bundleconfig.json");

    private static void WriteMessages(
        IReadOnlyList<BuildMessage> messages,
        TextWriter stdout,
        TextWriter stderr)
    {
        foreach (var message in messages)
        {
            var writer = message.Severity == BuildSeverity.Error ? stderr : stdout;
            writer.WriteLine(message.Message);
        }
    }

    private static void WriteHelp(TextWriter writer)
    {
        writer.WriteLine("WebBundler");
        writer.WriteLine("Usage:");
        writer.WriteLine("  webbundler build [--config <path>]");
        writer.WriteLine("  webbundler validate [--config <path>]");
        writer.WriteLine();
        writer.WriteLine("Commands:");
        writer.WriteLine("  build      Build bundles and write output files.");
        writer.WriteLine("  validate   Validate configuration and bundle inputs without writing files.");
        writer.WriteLine();
        writer.WriteLine("Options:");
        writer.WriteLine("  -c, --config <path>  Path to bundleconfig.json.");
        writer.WriteLine("  -h, --help           Show help.");
    }
}
