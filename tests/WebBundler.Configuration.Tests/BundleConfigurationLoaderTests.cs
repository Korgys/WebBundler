using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebBundler.Configuration;
using WebBundler.Core;

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
              "$schema": "https://raw.githubusercontent.com/korgys/WebBundler/main/schemas/bundleconfig.v1.schema.json",
              "version": 1,
              "manifestOutput": "wwwroot/dist/webbundler.manifest.json",
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
        Assert.AreEqual("wwwroot/dist/webbundler.manifest.json", result.Configuration.ManifestOutput);
        Assert.HasCount(1, result.Configuration.Bundles);
        Assert.AreEqual(BundleType.Css, result.Configuration.Bundles[0].Type);
        Assert.IsNotNull(result.Configuration.ExtensionData);
        Assert.IsTrue(result.Configuration.ExtensionData!.ContainsKey("$schema"));
    }

    [TestMethod]
    public void LoadsAdvancedConfigurationShape()
    {
        using var workspace = new TestWorkspace();
        var path = workspace.Write(
            "bundleconfig.json",
            """
            {
              "$schema": "https://raw.githubusercontent.com/korgys/WebBundler/main/schemas/bundleconfig.v1.schema.json",
              "version": 1,
              "bundles": [
                {
                  "output": "wwwroot/dist/site.min.js",
                  "inputs": [
                    "wwwroot/js/vendor/*.js",
                    "wwwroot/js/app.js"
                  ],
                  "type": "js",
                  "minify": false,
                  "fingerprint": true,
                  "sourceMap": true,
                  "environment": "Development",
                  "include": [ "wwwroot/js/**/*.js" ],
                  "exclude": [ "wwwroot/js/**/*.min.js" ]
                }
              ]
            }
            """);

        var loader = new BundleConfigurationLoader();
        var result = loader.Load(path);

        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(result.Configuration);
        var bundle = result.Configuration!.Bundles[0];
        Assert.IsFalse(bundle.Minify);
        Assert.IsTrue(bundle.Fingerprint.HasValue && bundle.Fingerprint.Value);
        Assert.IsTrue(bundle.SourceMap.HasValue && bundle.SourceMap.Value);
        Assert.AreEqual("Development", bundle.Environment);
        Assert.HasCount(1, bundle.Include!);
        Assert.HasCount(1, bundle.Exclude!);
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

    [TestMethod]
    public void LoadsUtf8ConfigurationWithBomAndUnicodeContent()
    {
        var path = Path.Combine(Path.GetTempPath(), "WebBundler.Tests", Guid.NewGuid().ToString("N"), "bundleconfig.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var json = "{\n" +
            "  \"version\": 1,\n" +
            "  \"bundles\": [\n" +
            "    {\n" +
            "      \"output\": \"wwwroot/dist/\u00fcber.site.css\",\n" +
            "      \"inputs\": [ \"wwwroot/css/ma\u00f1ana.css\" ],\n" +
            "      \"type\": \"css\",\n" +
            "      \"minify\": true\n" +
            "    }\n" +
            "  ]\n" +
            "}\n";

        File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var loader = new BundleConfigurationLoader();
        var result = loader.Load(path);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("wwwroot/dist/\u00fcber.site.css", result.Configuration!.Bundles[0].Output);
        Assert.AreEqual("wwwroot/css/ma\u00f1ana.css", result.Configuration.Bundles[0].Inputs[0]);
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
