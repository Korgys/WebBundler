using WebBundler.Core;
using WebBundler.Fingerprinting;
using WebBundler.Minification;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebBundler.Core.Tests;

[TestClass]
public sealed class BundleBuildServiceTests
{
    [TestMethod]
    public void BuildsCssAndJsBundlesInDeclaredOrder()
    {
        using var workspace = new TestWorkspace();
        workspace.Write("wwwroot/css/reset.css", "/* reset */\nbody { margin: 0; }\n");
        workspace.Write("wwwroot/css/site.css", "h1 { color: red; }\n");
        workspace.Write("wwwroot/js/lib/b.js", "// b\nwindow.b = 2;\n");
        workspace.Write("wwwroot/js/lib/a.js", "// a\nwindow.a = 1;\n");
        workspace.Write("wwwroot/js/site.js", "const value = 42;\n");

        var service = new BundleBuildService(DefaultAssetMinifiers.Create(), fileSystem: new PhysicalAssetFileSystem());
        var result = service.Build(new BundleBuildRequest(
            new BuildContext(workspace.Root),
            [
                new AssetBundleDefinition
                {
                    Output = "wwwroot/dist/site.min.css",
                    Inputs = ["wwwroot/css/reset.css", "wwwroot/css/site.css"],
                    Type = BundleType.Css
                },
                new AssetBundleDefinition
                {
                    Output = "wwwroot/dist/site.min.js",
                    Inputs = ["wwwroot/js/lib/*.js", "wwwroot/js/site.js"],
                    Type = BundleType.JavaScript
                }
            ]));

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(2, result.Outputs);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css")), result.Outputs[0].OutputPath);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(workspace.Root, "wwwroot/dist/site.min.js")), result.Outputs[1].OutputPath);
        var css = File.ReadAllText(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css"));
        StringAssert.Contains(css, "body{margin:0}");
        StringAssert.Contains(css, "h1{color:red}");
        StringAssert.Contains(File.ReadAllText(Path.Combine(workspace.Root, "wwwroot/dist/site.min.js")), "window.a = 1;");
        StringAssert.Contains(File.ReadAllText(Path.Combine(workspace.Root, "wwwroot/dist/site.min.js")), "window.b = 2;");
    }

    [TestMethod]
    public void WildcardsResolveDeterministically()
    {
        using var workspace = new TestWorkspace();
        workspace.Write("assets/c.js", "window.c = true;");
        workspace.Write("assets/a.js", "window.a = true;");
        workspace.Write("assets/b.js", "window.b = true;");

        var service = new BundleBuildService(DefaultAssetMinifiers.Create(), fileSystem: new PhysicalAssetFileSystem());
        var result = service.Build(new BundleBuildRequest(
            new BuildContext(workspace.Root),
            [
                new AssetBundleDefinition
                {
                    Output = "dist/site.js",
                    Inputs = ["assets/*.js"],
                    Type = BundleType.JavaScript
                }
            ]));

        Assert.IsTrue(result.Succeeded);
        var output = File.ReadAllText(Path.Combine(workspace.Root, "dist/site.js"));
        var first = output.IndexOf("window.a", StringComparison.Ordinal);
        var second = output.IndexOf("window.b", StringComparison.Ordinal);
        var third = output.IndexOf("window.c", StringComparison.Ordinal);
        Assert.IsTrue(first < second && second < third);
    }

    [TestMethod]
    public void MissingInputsProduceAnError()
    {
        using var workspace = new TestWorkspace();
        workspace.Write("assets/site.css", "body { color: black; }");

        var service = new BundleBuildService(DefaultAssetMinifiers.Create(), fileSystem: new PhysicalAssetFileSystem());
        var result = service.Build(new BundleBuildRequest(
            new BuildContext(workspace.Root),
            [
                new AssetBundleDefinition
                {
                    Output = "dist/site.css",
                    Inputs = ["assets/missing.css"],
                    Type = BundleType.Css
                }
            ],
            WriteOutputs: false));

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.Messages.Any(message => message.Severity == BuildSeverity.Error));
    }

    [TestMethod]
    public void BuildIsDeterministicAcrossRuns()
    {
        using var workspace = new TestWorkspace();
        workspace.Write("assets/site.css", "body { color: black; }\n");

        var service = new BundleBuildService(DefaultAssetMinifiers.Create(), fileSystem: new PhysicalAssetFileSystem());
        var request = new BundleBuildRequest(
            new BuildContext(workspace.Root),
            [
                new AssetBundleDefinition
                {
                    Output = "dist/site.css",
                    Inputs = ["assets/site.css"],
                    Type = BundleType.Css
                }
            ]);

        var first = service.Build(request);
        var second = service.Build(request);

        Assert.IsTrue(first.Succeeded);
        Assert.IsTrue(second.Succeeded);
        Assert.AreEqual(first.Outputs[0].ContentHash, second.Outputs[0].ContentHash);
    }

    [TestMethod]
    public void ReturnsAnErrorWhenMinifyingWithoutARegisteredMinifier()
    {
        using var workspace = new TestWorkspace();
        workspace.Write("assets/site.js", "window.site = true;");

        var service = new BundleBuildService(Array.Empty<IAssetMinifier>(), fileSystem: new PhysicalAssetFileSystem());
        var result = service.Build(new BundleBuildRequest(
            new BuildContext(workspace.Root),
            [
                new AssetBundleDefinition
                {
                    Output = "dist/site.js",
                    Inputs = ["assets/site.js"],
                    Type = BundleType.JavaScript
                }
            ],
            WriteOutputs: false));

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.Messages.Any(message => message.Severity == BuildSeverity.Error));
    }

    [TestMethod]
    public void EmitsAWarningWhenFingerprintingIsRequestedWithoutAFingerprinter()
    {
        using var workspace = new TestWorkspace();
        workspace.Write("assets/site.css", "body { color: red; }\n");

        var service = new BundleBuildService(DefaultAssetMinifiers.Create(), fileSystem: new PhysicalAssetFileSystem());
        var result = service.Build(new BundleBuildRequest(
            new BuildContext(workspace.Root),
            [
                new AssetBundleDefinition
                {
                    Output = "dist/site.css",
                    Inputs = ["assets/site.css"],
                    Type = BundleType.Css,
                    Fingerprint = true
                }
            ],
            WriteOutputs: false));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(workspace.Root, "dist/site.css")), result.Outputs[0].OutputPath);
        Assert.IsTrue(result.Messages.Any(message => message.Severity == BuildSeverity.Warning));
    }

    [TestMethod]
    public void RejectsDuplicateOutputsAfterPathNormalization()
    {
        using var workspace = new TestWorkspace();
        workspace.Write("assets/a.css", "body { color: red; }\n");

        var service = new BundleBuildService(DefaultAssetMinifiers.Create(), fileSystem: new PhysicalAssetFileSystem());
        var result = service.Build(new BundleBuildRequest(
            new BuildContext(workspace.Root),
            [
                new AssetBundleDefinition
                {
                    Output = "dist/site.css",
                    Inputs = ["assets/a.css"],
                    Type = BundleType.Css
                },
                new AssetBundleDefinition
                {
                    Output = Path.Combine("dist", ".", "site.css"),
                    Inputs = ["assets/a.css"],
                    Type = BundleType.Css
                }
            ],
            WriteOutputs: false));

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.Messages.Any(message => message.Severity == BuildSeverity.Error));
        Assert.HasCount(1, result.Outputs);
    }

    private sealed class TestWorkspace : IDisposable
    {
        public TestWorkspace()
        {
            Root = Path.Combine(Path.GetTempPath(), "WebBundler.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void Write(string relativePath, string content)
        {
            var path = Path.Combine(Root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
