using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebBundler.Core;

namespace WebBundler.Core.Tests;

[TestClass]
public sealed class AssetManifestServiceTests
{
    [TestMethod]
    public void CreateOrdersBundlesByNormalizedLogicalPath()
    {
        var root = CreateRootPath();
        var service = new AssetManifestService();

        var manifest = service.Create(
            root,
            [
                new AssetOutput(Path.Combine(root, "wwwroot/dist/z.js"), [])
                {
                    LogicalOutputPath = "wwwroot/dist/z.js",
                    Type = BundleType.JavaScript
                },
                new AssetOutput(Path.Combine(root, "wwwroot/dist/a.css"), [])
                {
                    LogicalOutputPath = "wwwroot/dist/a.css",
                    Type = BundleType.Css
                }
            ]);

        CollectionAssert.AreEqual(
            new[] { "wwwroot/dist/a.css", "wwwroot/dist/z.js" },
            manifest.Bundles.Keys.ToArray());
    }

    [TestMethod]
    public void CreateNormalizesAbsoluteLogicalAndGeneratedPaths()
    {
        var root = CreateRootPath();
        var service = new AssetManifestService();
        var logicalPath = Path.Combine(root, "wwwroot", "dist", "site.min.css");
        var generatedPath = Path.Combine(root, "wwwroot", "dist", "site.min.12345678.css");

        var manifest = service.Create(
            root,
            [
                new AssetOutput(generatedPath, [], Fingerprint: "12345678")
                {
                    LogicalOutputPath = logicalPath,
                    Type = BundleType.Css
                }
            ]);

        var entry = manifest.Bundles["wwwroot/dist/site.min.css"];
        Assert.AreEqual("wwwroot/dist/site.min.12345678.css", entry.Output);
        Assert.IsTrue(entry.Fingerprinted);
        Assert.AreEqual("css", entry.Type);
    }

    [TestMethod]
    public void CreateWritesJavaScriptType()
    {
        var root = CreateRootPath();
        var service = new AssetManifestService();

        var manifest = service.Create(
            root,
            [
                new AssetOutput(Path.Combine(root, "dist/app.js"), [])
                {
                    LogicalOutputPath = "dist/app.js",
                    Type = BundleType.JavaScript
                }
            ]);

        Assert.AreEqual("js", manifest.Bundles["dist/app.js"].Type);
    }

    [TestMethod]
    public void CreateRejectsMissingLogicalOutputPath()
    {
        var service = new AssetManifestService();

        AssertThrows<InvalidOperationException>(() =>
            service.Create(CreateRootPath(), [new AssetOutput("dist/site.css", []) { Type = BundleType.Css }]));
    }

    [TestMethod]
    public void CreateRejectsMissingBundleType()
    {
        var service = new AssetManifestService();

        AssertThrows<InvalidOperationException>(() =>
            service.Create(
                CreateRootPath(),
                [new AssetOutput("dist/site.css", []) { LogicalOutputPath = "dist/site.css" }]));
    }

    [TestMethod]
    public void CreateRejectsUnsupportedBundleType()
    {
        var service = new AssetManifestService();

        AssertThrows<InvalidOperationException>(() =>
            service.Create(
                CreateRootPath(),
                [
                    new AssetOutput("dist/site.css", [])
                    {
                        LogicalOutputPath = "dist/site.css",
                        Type = (BundleType)99
                    }
                ]));
    }

    [TestMethod]
    public void SerializeUsesCamelCaseProperties()
    {
        var service = new AssetManifestService();
        var manifest = new AssetManifest(
            1,
            new Dictionary<string, AssetManifestEntry>
            {
                ["dist/site.css"] = new("dist/site.12345678.css", Fingerprinted: true, Type: "css")
            });

        var json = service.Serialize(manifest);

        using var document = JsonDocument.Parse(json);
        Assert.AreEqual(1, document.RootElement.GetProperty("version").GetInt32());
        Assert.IsTrue(document.RootElement.GetProperty("bundles").GetProperty("dist/site.css").GetProperty("fingerprinted").GetBoolean());
    }

    [TestMethod]
    public void CreateRejectsNullArguments()
    {
        var service = new AssetManifestService();

        AssertThrows<ArgumentNullException>(() => service.Create(null!, []));
        AssertThrows<ArgumentNullException>(() => service.Create(CreateRootPath(), null!));
    }

    private static void AssertThrows<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        Assert.Fail($"Expected exception of type {typeof(TException).FullName}.");
    }

    private static string CreateRootPath() =>
        Path.Combine(Path.GetTempPath(), "WebBundler.Tests", Guid.NewGuid().ToString("N"));
}
