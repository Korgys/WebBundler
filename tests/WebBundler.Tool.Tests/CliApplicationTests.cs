using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebBundler.Tool;

namespace WebBundler.Tool.Tests;

[TestClass]
public sealed class CliApplicationTests
{
    [TestMethod]
    public void NoArgumentsShowHelp()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = new CliApplication().Run([], stdout, stderr);

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(stdout.ToString(), "Usage:");
        Assert.AreEqual(string.Empty, stderr.ToString());
    }

    [TestMethod]
    public void UnknownCommandReturnsAnError()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = new CliApplication().Run(["bogus"], stdout, stderr);

        Assert.AreEqual(1, exitCode);
        StringAssert.Contains(stderr.ToString(), "Unknown command 'bogus'.");
        StringAssert.Contains(stdout.ToString(), "Usage:");
    }

    [TestMethod]
    public void MissingConfigurationValueReturnsAnError()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = new CliApplication().Run(["build", "--config"], stdout, stderr);

        Assert.AreEqual(1, exitCode);
        StringAssert.Contains(stderr.ToString(), "Missing value for --config.");
    }

    [TestMethod]
    public void MissingConfigurationFileReturnsTwo()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var missingConfig = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "bundleconfig.json");

        var exitCode = new CliApplication().Run(["build", "--config", missingConfig], stdout, stderr);

        Assert.AreEqual(2, exitCode);
        StringAssert.Contains(stderr.ToString(), "was not found");
    }

    [TestMethod]
    public void BuildCommandWritesOutputs()
    {
        using var workspace = new TestWorkspace();
        workspace.Write(
            "bundleconfig.json",
            """
            {
              "version": 1,
              "bundles": [
                {
                  "output": "wwwroot/dist/site.min.css",
                  "inputs": [ "wwwroot/css/site.css" ],
                  "type": "css",
                  "minify": true,
                  "sourceMap": true
                },
                {
                  "output": "wwwroot/dist/site.min.js",
                  "inputs": [ "wwwroot/js/site.js" ],
                  "type": "js",
                  "minify": true,
                  "sourceMap": true
                }
              ]
            }
            """);
        workspace.Write("wwwroot/css/site.css", "body { color: red; }\n");
        workspace.Write("wwwroot/js/site.js", "const value = 1; // comment\n");

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = new CliApplication().Run(["build", "--config", workspace.ConfigPath], stdout, stderr);

        Assert.AreEqual(0, exitCode);
        Assert.IsTrue(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css")));
        Assert.IsTrue(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/site.min.js")));
        Assert.IsTrue(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css.map")));
        Assert.IsTrue(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/site.min.js.map")));
        StringAssert.Contains(File.ReadAllText(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css")), "body{color:red}");
        StringAssert.Contains(File.ReadAllText(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css")), "sourceMappingURL=site.min.css.map");
        StringAssert.Contains(File.ReadAllText(Path.Combine(workspace.Root, "wwwroot/dist/site.min.js")), "const value = 1;");
        StringAssert.Contains(File.ReadAllText(Path.Combine(workspace.Root, "wwwroot/dist/site.min.js")), "sourceMappingURL=site.min.js.map");
    }

    [TestMethod]
    public void ValidateCommandDoesNotWriteOutputs()
    {
        using var workspace = new TestWorkspace();
        workspace.Write(
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
        workspace.Write("wwwroot/css/site.css", "body { color: red; }\n");

        var exitCode = new CliApplication().Run(["validate", "--config", workspace.ConfigPath], new StringWriter(), new StringWriter());

        Assert.AreEqual(0, exitCode);
        Assert.IsFalse(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css")));
    }

    [TestMethod]
    public void CheckCommandDoesNotWriteSourceMaps()
    {
        using var workspace = new TestWorkspace();
        workspace.Write(
            "bundleconfig.json",
            """
            {
              "version": 1,
              "bundles": [
                {
                  "output": "wwwroot/dist/site.min.css",
                  "inputs": [ "wwwroot/css/site.css" ],
                  "type": "css",
                  "minify": true,
                  "sourceMap": true
                }
              ]
            }
            """);
        workspace.Write("wwwroot/css/site.css", "body { color: red; }\n");

        var exitCode = new CliApplication().Run(["check", "--config", workspace.ConfigPath], new StringWriter(), new StringWriter());

        Assert.AreEqual(0, exitCode);
        Assert.IsFalse(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css.map")));
    }

    [TestMethod]
    public void CheckCommandReturnsThreeWhenBundleBuildFails()
    {
        using var workspace = new TestWorkspace();
        workspace.Write(
            "bundleconfig.json",
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

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = new CliApplication().Run(["check", "--config", workspace.ConfigPath], stdout, stderr);

        Assert.AreEqual(3, exitCode);
        StringAssert.Contains(stderr.ToString(), "did not match any files");
    }

    private sealed class TestWorkspace : IDisposable
    {
        public TestWorkspace()
        {
            Root = Path.Combine(Path.GetTempPath(), "WebBundler.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public string ConfigPath => Path.Combine(Root, "bundleconfig.json");

        public string Write(string relativePath, string content)
        {
            var path = Path.Combine(Root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
            return path;
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
