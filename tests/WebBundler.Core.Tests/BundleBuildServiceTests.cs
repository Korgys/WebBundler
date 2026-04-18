using WebBundler.Core;
using WebBundler.Minification;
using Xunit;

namespace WebBundler.Core.Tests;

public sealed class BundleBuildServiceTests
{
    [Fact]
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

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Outputs.Count);
        Assert.Equal(Path.GetFullPath(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css")), result.Outputs[0].OutputPath);
        Assert.Equal(Path.GetFullPath(Path.Combine(workspace.Root, "wwwroot/dist/site.min.js")), result.Outputs[1].OutputPath);
        var css = File.ReadAllText(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css"));
        Assert.Contains("body{margin:0}", css);
        Assert.Contains("h1{color:red}", css);
        Assert.Contains("window.a = 1;", File.ReadAllText(Path.Combine(workspace.Root, "wwwroot/dist/site.min.js")));
        Assert.Contains("window.b = 2;", File.ReadAllText(Path.Combine(workspace.Root, "wwwroot/dist/site.min.js")));
    }

    [Fact]
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

        Assert.True(result.Succeeded);
        var output = File.ReadAllText(Path.Combine(workspace.Root, "dist/site.js"));
        var first = output.IndexOf("window.a", StringComparison.Ordinal);
        var second = output.IndexOf("window.b", StringComparison.Ordinal);
        var third = output.IndexOf("window.c", StringComparison.Ordinal);
        Assert.True(first < second && second < third);
    }

    [Fact]
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

        Assert.False(result.Succeeded);
        Assert.Contains(result.Messages, message => message.Severity == BuildSeverity.Error);
    }

    [Fact]
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

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(first.Outputs[0].ContentHash, second.Outputs[0].ContentHash);
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
