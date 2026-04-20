using WebBundler.Configuration;
using WebBundler.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebBundler.Configuration.Tests;

[TestClass]
public sealed class BundleConfigurationLoaderTests
{
    [TestMethod]
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

        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(result.Configuration);
        Assert.AreEqual(1, result.Configuration!.Version);
        Assert.HasCount(1, result.Configuration.Bundles);
        Assert.AreEqual(BundleType.Css, result.Configuration.Bundles[0].Type);
    }

    [TestMethod]
    public void ReportsInvalidJson()
    {
        using var workspace = new TestWorkspace();
        var path = workspace.Write("bundleconfig.json", "{ invalid json ");

        var loader = new BundleConfigurationLoader();
        var result = loader.Load(path);

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.Messages.Any(message => message.Severity == BuildSeverity.Error));
    }

    [TestMethod]
    public void ReportsMissingConfigurationFile()
    {
        var loader = new BundleConfigurationLoader();
        var result = loader.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "bundleconfig.json"));

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.Messages.Any(message => message.Severity == BuildSeverity.Error));
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
