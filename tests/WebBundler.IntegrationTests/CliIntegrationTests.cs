using WebBundler.Tool;
using Xunit;

namespace WebBundler.IntegrationTests;

public sealed class CliIntegrationTests
{
    [Fact]
    public void BuildCommandWritesOutputsForSampleAssets()
    {
        using var workspace = new SampleWorkspace("RazorSample");
        var cli = new CliApplication();

        var exitCode = cli.Run(["build", "--config", workspace.BundleConfigPath], new StringWriter(), new StringWriter());

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css")));
        Assert.True(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/site.min.js")));
    }

    [Fact]
    public void ValidateCommandDoesNotWriteOutputs()
    {
        using var workspace = new SampleWorkspace("AspNetMvcSample");
        var cli = new CliApplication();

        var exitCode = cli.Run(["validate", "--config", workspace.BundleConfigPath], new StringWriter(), new StringWriter());

        Assert.Equal(0, exitCode);
        Assert.False(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css")));
        Assert.False(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/site.min.js")));
    }

    [Fact]
    public void CheckCommandDoesNotWriteOutputs()
    {
        using var workspace = new SampleWorkspace("AspNetMvcSample");
        var cli = new CliApplication();

        var exitCode = cli.Run(["check", "--config", workspace.BundleConfigPath], new StringWriter(), new StringWriter());

        Assert.Equal(0, exitCode);
        Assert.False(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css")));
        Assert.False(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/site.min.js")));
    }

    [Fact]
    public void CheckCommandReturnsBuildFailureForMissingInputs()
    {
        using var workspace = new SampleWorkspace("AspNetMvcSample");
        File.WriteAllText(
            workspace.BundleConfigPath,
            """
            {
              "version": 1,
              "bundles": [
                {
                  "output": "wwwroot/dist/site.min.css",
                  "inputs": [ "wwwroot/css/missing.css" ],
                  "type": "css",
                  "minify": true
                }
              ]
            }
            """);

        var cli = new CliApplication();
        var exitCode = cli.Run(["check", "--config", workspace.BundleConfigPath], new StringWriter(), new StringWriter());

        Assert.Equal(3, exitCode);
        Assert.False(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css")));
    }

    [Fact]
    public void MissingConfigurationReturnsNonZeroExitCode()
    {
        var cli = new CliApplication();
        var exitCode = cli.Run(["build", "--config", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "bundleconfig.json")], new StringWriter(), new StringWriter());

        Assert.Equal(2, exitCode);
    }

    private sealed class SampleWorkspace : IDisposable
    {
        public SampleWorkspace(string sampleName)
        {
            var repoRoot = FindRepositoryRoot();
            var sampleRoot = Path.Combine(repoRoot, "samples", sampleName);
            Root = Path.Combine(Path.GetTempPath(), "WebBundler.Tests", Guid.NewGuid().ToString("N"));
            CopyDirectory(sampleRoot, Root);
            BundleConfigPath = Path.Combine(Root, "bundleconfig.json");
        }

        public string Root { get; }

        public string BundleConfigPath { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "WebBundler.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate repository root.");
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(directory.Replace(source, destination, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var target = file.Replace(source, destination, StringComparison.OrdinalIgnoreCase);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, overwrite: true);
            }
        }
    }
}
