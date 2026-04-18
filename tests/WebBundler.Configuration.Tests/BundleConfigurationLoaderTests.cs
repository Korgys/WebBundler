using WebBundler.Configuration;
using WebBundler.Core;
using Xunit;

namespace WebBundler.Configuration.Tests;

public sealed class BundleConfigurationLoaderTests
{
    [Fact]
    public void LoadsConfigurationDocument()
    {
        using var workspace = new TestWorkspace();
        var path = workspace.Write(
            "bundleconfig.json",
            """
            {
              "version": 1,
              "bundles": [
                {
                  "output": "wwwroot/dist/site.min.css",
                  "inputs": [ "wwwroot/css/site.css" ],
                  "type": "css",
                  "minify": true
                }
              ]
            }
            """);

        var loader = new BundleConfigurationLoader();
        var result = loader.Load(path);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Configuration);
        Assert.Equal(1, result.Configuration!.Version);
        Assert.Single(result.Configuration.Bundles);
        Assert.Equal(BundleType.Css, result.Configuration.Bundles[0].Type);
    }

    [Fact]
    public void ReportsInvalidJson()
    {
        using var workspace = new TestWorkspace();
        var path = workspace.Write("bundleconfig.json", "{ invalid json ");

        var loader = new BundleConfigurationLoader();
        var result = loader.Load(path);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Messages, message => message.Severity == BuildSeverity.Error);
    }

    private sealed class TestWorkspace : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), "WebBundler.Tests", Guid.NewGuid().ToString("N"));

        public TestWorkspace()
        {
            Directory.CreateDirectory(root);
        }

        public string Write(string relativePath, string content)
        {
            var path = Path.Combine(root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
