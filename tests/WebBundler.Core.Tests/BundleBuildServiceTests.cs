using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
                    Type = BundleType.Css,
                    SourceMap = true
                },
                new AssetBundleDefinition
                {
                    Output = "wwwroot/dist/site.min.js",
                    Inputs = ["wwwroot/js/lib/*.js", "wwwroot/js/site.js"],
                    Type = BundleType.JavaScript,
                    SourceMap = true
                }
            ]));

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(2, result.Outputs);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css")), result.Outputs[0].OutputPath);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(workspace.Root, "wwwroot/dist/site.min.js")), result.Outputs[1].OutputPath);
        var css = File.ReadAllText(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css"));
        StringAssert.Contains(css, "body{margin:0}");
        StringAssert.Contains(css, "h1{color:red}");
        var js = File.ReadAllText(Path.Combine(workspace.Root, "wwwroot/dist/site.min.js"));
        StringAssert.Contains(js, "window.a = 1;");
        StringAssert.Contains(js, "window.b = 2;");
        StringAssert.Contains(css, "sourceMappingURL=site.min.css.map");
        StringAssert.Contains(js, "sourceMappingURL=site.min.js.map");

        using var cssMap = JsonDocument.Parse(File.ReadAllText(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css.map")));
        using var jsMap = JsonDocument.Parse(File.ReadAllText(Path.Combine(workspace.Root, "wwwroot/dist/site.min.js.map")));

        CollectionAssert.AreEqual(
            new[] { "../css/reset.css", "../css/site.css" },
            cssMap.RootElement.GetProperty("sources").EnumerateArray().Select(element => element.GetString()).ToArray());
        CollectionAssert.AreEqual(
            new[] { "../js/lib/a.js", "../js/lib/b.js", "../js/site.js" },
            jsMap.RootElement.GetProperty("sources").EnumerateArray().Select(element => element.GetString()).ToArray());
    }

    [TestMethod]
    public void WritesManifestForFingerprintedBundles()
    {
        using var workspace = new TestWorkspace();
        workspace.Write("wwwroot/js/site.js", "window.site = true;\n");

        var service = new BundleBuildService(
            DefaultAssetMinifiers.Create(),
            new Sha256AssetFingerprinter(),
            new PhysicalAssetFileSystem());

        var result = service.Build(new BundleBuildRequest(
            new BuildContext(workspace.Root),
            [
                new AssetBundleDefinition
                {
                    Output = "wwwroot/dist/site.min.js",
                    Inputs = ["wwwroot/js/site.js"],
                    Type = BundleType.JavaScript,
                    Minify = false,
                    Fingerprint = true,
                    SourceMap = true
                }
            ],
            ManifestOutput: "wwwroot/dist/webbundler.manifest.json"));

        Assert.IsTrue(result.Succeeded);

        var manifestPath = Path.Combine(workspace.Root, "wwwroot/dist/webbundler.manifest.json");
        Assert.IsTrue(File.Exists(manifestPath));

        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var bundles = manifest.RootElement.GetProperty("bundles");
        Assert.AreEqual(1, bundles.EnumerateObject().Count());

        var entry = bundles.GetProperty("wwwroot/dist/site.min.js");
        Assert.IsTrue(entry.GetProperty("fingerprinted").GetBoolean());
        Assert.AreEqual("js", entry.GetProperty("type").GetString());
        Assert.IsFalse(Path.IsPathRooted(entry.GetProperty("output").GetString()!));
        Assert.IsFalse(entry.GetProperty("output").GetString()!.Contains('\\'));

        var fingerprintedFiles = Directory.GetFiles(Path.Combine(workspace.Root, "wwwroot/dist"), "site.min.*.js");
        Assert.HasCount(1, fingerprintedFiles);
        var fingerprintedContent = File.ReadAllText(fingerprintedFiles[0]);
        var expectedFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintedContent))).ToLowerInvariant()[..8];
        Assert.AreEqual($"wwwroot/dist/site.min.{expectedFingerprint}.js", entry.GetProperty("output").GetString());

        var sourceMapPath = Path.Combine(workspace.Root, "wwwroot/dist/site.min.js.map");
        Assert.IsTrue(File.Exists(sourceMapPath));
        StringAssert.Contains(File.ReadAllText(fingerprintedFiles[0]), "sourceMappingURL=site.min.js.map");

        using var map = JsonDocument.Parse(File.ReadAllText(sourceMapPath));
        Assert.AreEqual(Path.GetFileName(fingerprintedFiles[0]), map.RootElement.GetProperty("file").GetString());
        CollectionAssert.AreEqual(
            new[] { "../js/site.js" },
            map.RootElement.GetProperty("sources").EnumerateArray().Select(element => element.GetString()).ToArray());

        var firstMapContent = File.ReadAllText(sourceMapPath);
        var secondResult = service.Build(new BundleBuildRequest(
            new BuildContext(workspace.Root),
            [
                new AssetBundleDefinition
                {
                    Output = "wwwroot/dist/site.min.js",
                    Inputs = ["wwwroot/js/site.js"],
                    Type = BundleType.JavaScript,
                    Minify = false,
                    Fingerprint = true,
                    SourceMap = true
                }
            ],
            ManifestOutput: "wwwroot/dist/webbundler.manifest.json"));

        Assert.IsTrue(secondResult.Succeeded);
        Assert.AreEqual(firstMapContent, File.ReadAllText(sourceMapPath));
    }

    [TestMethod]
    public void WritesManifestWithoutFingerprinting()
    {
        using var workspace = new TestWorkspace();
        workspace.Write("wwwroot/css/site.css", "body { color: black; }\n");

        var service = new BundleBuildService(DefaultAssetMinifiers.Create(), fileSystem: new PhysicalAssetFileSystem());
        var result = service.Build(new BundleBuildRequest(
            new BuildContext(workspace.Root),
            [
                new AssetBundleDefinition
                {
                    Output = "wwwroot/dist/../dist/site.min.css",
                    Inputs = ["wwwroot/css/site.css"],
                    Type = BundleType.Css,
                    Minify = false
                }
            ],
            ManifestOutput: "wwwroot/dist/webbundler.manifest.json"));

        Assert.IsTrue(result.Succeeded);

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(workspace.Root, "wwwroot/dist/webbundler.manifest.json")));
        var entry = manifest.RootElement.GetProperty("bundles").GetProperty("wwwroot/dist/site.min.css");
        Assert.IsFalse(entry.GetProperty("fingerprinted").GetBoolean());
        Assert.AreEqual("css", entry.GetProperty("type").GetString());
        Assert.AreEqual("wwwroot/dist/site.min.css", entry.GetProperty("output").GetString());
        Assert.IsFalse(Path.IsPathRooted(entry.GetProperty("output").GetString()!));
        Assert.IsFalse(entry.GetProperty("output").GetString()!.Contains('\\'));
    }

    [TestMethod]
    public void CheckModeDoesNotWriteManifest()
    {
        using var workspace = new TestWorkspace();
        workspace.Write("wwwroot/css/site.css", "body { color: black; }\n");

        var service = new BundleBuildService(DefaultAssetMinifiers.Create(), fileSystem: new PhysicalAssetFileSystem());
        var result = service.Build(new BundleBuildRequest(
            new BuildContext(workspace.Root),
            [
                new AssetBundleDefinition
                {
                    Output = "wwwroot/dist/site.min.css",
                    Inputs = ["wwwroot/css/site.css"],
                    Type = BundleType.Css,
                    Minify = false,
                    SourceMap = true
                }
            ],
            WriteOutputs: false,
            ManifestOutput: "wwwroot/dist/webbundler.manifest.json"));

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/webbundler.manifest.json")));
        Assert.IsFalse(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css.map")));
    }

    [TestMethod]
    public void ManifestContentIsDeterministicAndOrdered()
    {
        using var workspace = new TestWorkspace();
        workspace.Write("assets/a.css", "body { color: red; }\n");
        workspace.Write("assets/z.js", "window.z = true;\n");

        var service = new BundleBuildService(DefaultAssetMinifiers.Create(), fileSystem: new PhysicalAssetFileSystem());
        var request = new BundleBuildRequest(
            new BuildContext(workspace.Root),
            [
                new AssetBundleDefinition
                {
                    Output = "wwwroot/dist/z.js",
                    Inputs = ["assets/z.js"],
                    Type = BundleType.JavaScript,
                    Minify = false
                },
                new AssetBundleDefinition
                {
                    Output = "wwwroot/dist/a.css",
                    Inputs = ["assets/a.css"],
                    Type = BundleType.Css,
                    Minify = false
                }
            ],
            ManifestOutput: "wwwroot/dist/webbundler.manifest.json");

        var first = service.Build(request);
        var firstContent = File.ReadAllText(Path.Combine(workspace.Root, "wwwroot/dist/webbundler.manifest.json"));
        var second = service.Build(request);
        var secondContent = File.ReadAllText(Path.Combine(workspace.Root, "wwwroot/dist/webbundler.manifest.json"));

        Assert.IsTrue(first.Succeeded);
        Assert.IsTrue(second.Succeeded);
        Assert.AreEqual(firstContent, secondContent);

        using var manifest = JsonDocument.Parse(firstContent);
        var bundleNames = manifest.RootElement.GetProperty("bundles").EnumerateObject().Select(property => property.Name).ToArray();
        CollectionAssert.AreEqual(
            new[] { "wwwroot/dist/a.css", "wwwroot/dist/z.js" },
            bundleNames);

        foreach (var property in manifest.RootElement.GetProperty("bundles").EnumerateObject())
        {
            var output = property.Value.GetProperty("output").GetString()!;
            Assert.IsFalse(Path.IsPathRooted(output));
            Assert.IsFalse(output.Contains('\\'));
        }
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
    public void SupportsNestedBundlesAndRecursiveGlobsWithWindowsStyleSeparators()
    {
        using var workspace = new TestWorkspace();
        workspace.Write("assets/vendor/root.js", "window.root = true;\n");
        workspace.Write("assets/vendor/nested/deep.js", "window.deep = true;\n");
        workspace.Write("assets/app.js", "window.app = true;\n");

        var service = new BundleBuildService(DefaultAssetMinifiers.Create(), fileSystem: new PhysicalAssetFileSystem());
        var result = service.Build(new BundleBuildRequest(
            new BuildContext(workspace.Root),
            [
                new AssetBundleDefinition
                {
                    Output = "dist/vendor.bundle.js",
                    Inputs = [@"assets\vendor\*.js", "assets/vendor/**/*.js"],
                    Type = BundleType.JavaScript,
                    Minify = false
                },
                new AssetBundleDefinition
                {
                    Output = "dist/app.bundle.js",
                    Inputs = ["dist/vendor.bundle.js", @"assets\app.js"],
                    Type = BundleType.JavaScript,
                    Minify = false
                }
            ]));

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(2, result.Outputs);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(workspace.Root, "dist/vendor.bundle.js")), result.Outputs[0].OutputPath);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(workspace.Root, "dist/app.bundle.js")), result.Outputs[1].OutputPath);
        Assert.HasCount(2, result.Outputs[0].SourceFiles);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(workspace.Root, "assets/vendor/root.js")), result.Outputs[0].SourceFiles[0]);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(workspace.Root, "assets/vendor/nested/deep.js")), result.Outputs[0].SourceFiles[1]);
        StringAssert.Contains(File.ReadAllText(Path.Combine(workspace.Root, "dist/vendor.bundle.js")), "window.root = true;");
        StringAssert.Contains(File.ReadAllText(Path.Combine(workspace.Root, "dist/vendor.bundle.js")), "window.deep = true;");
        StringAssert.Contains(File.ReadAllText(Path.Combine(workspace.Root, "dist/app.bundle.js")), "window.app = true;");
        StringAssert.Contains(File.ReadAllText(Path.Combine(workspace.Root, "dist/app.bundle.js")), "window.root = true;");
    }

    [TestMethod]
    public void HandlesLargeInputsWithoutChangingContent()
    {
        using var workspace = new TestWorkspace();
        var largeContent = new string('a', 1_000_000);
        workspace.Write("assets/large.css", largeContent);

        var service = new BundleBuildService(DefaultAssetMinifiers.Create(), fileSystem: new PhysicalAssetFileSystem());
        var result = service.Build(new BundleBuildRequest(
            new BuildContext(workspace.Root),
            [
                new AssetBundleDefinition
                {
                    Output = "dist/large.css",
                    Inputs = ["assets/large.css"],
                    Type = BundleType.Css,
                    Minify = false
                }
        ]));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual((long)largeContent.Length, result.Outputs[0].Length);
        Assert.AreEqual(largeContent, File.ReadAllText(Path.Combine(workspace.Root, "dist/large.css")));
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
                    Output = Path.Combine("dist", "..", "dist", "site.css"),
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
