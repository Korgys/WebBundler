using System.Text.Json;
using WebBundler.Tool;
using System.Security.Cryptography;
using System.Text;

namespace WebBundler.IntegrationTests;

[TestClass]
public sealed class CliIntegrationTests
{
    [TestMethod]
    public void BuildCommandWritesOutputsForSampleAssets()
    {
        using var workspace = new SampleWorkspace("RazorSample");
        var cli = new CliApplication();

        var exitCode = cli.Run(["build", "--config", workspace.BundleConfigPath], new StringWriter(), new StringWriter());

        Assert.AreEqual(0, exitCode);
        Assert.IsTrue(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css")));
        var fingerprintedFiles = Directory.GetFiles(Path.Combine(workspace.Root, "wwwroot/dist"), "site.min.*.js");
        Assert.HasCount(1, fingerprintedFiles);

        var fingerprintedPath = fingerprintedFiles[0];
        var fingerprintedContent = File.ReadAllText(fingerprintedPath);
        var expectedFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintedContent))).ToLowerInvariant()[..8];
        Assert.AreEqual($"site.min.{expectedFingerprint}.js", Path.GetFileName(fingerprintedPath));
    }

    [TestMethod]
    public void BuildCommandWritesManifestForFingerprintedBundles()
    {
        using var workspace = new SampleWorkspace("RazorSample");
        File.WriteAllText(
            workspace.BundleConfigPath,
            """
            {
              "$schema": "https://raw.githubusercontent.com/korgys/WebBundler/main/schemas/bundleconfig.v1.schema.json",
              "version": 1,
              "manifestOutput": "wwwroot/dist/webbundler.manifest.json",
              "bundles": [
                {
                  "output": "wwwroot/dist/site.min.css",
                  "inputs": [
                    "wwwroot/css/reset.css",
                    "wwwroot/css/site.css"
                  ],
                  "type": "css",
                  "minify": true
                },
                {
                  "output": "wwwroot/dist/site.min.js",
                  "inputs": [
                    "wwwroot/js/lib/*.js",
                    "wwwroot/js/site.js"
                  ],
                  "type": "js",
                  "minify": true,
                  "fingerprint": true
                }
              ]
            }
            """);

        var cli = new CliApplication();
        var exitCode = cli.Run(["build", "--config", workspace.BundleConfigPath], new StringWriter(), new StringWriter());

        Assert.AreEqual(0, exitCode);
        var manifestPath = Path.Combine(workspace.Root, "wwwroot/dist/webbundler.manifest.json");
        Assert.IsTrue(File.Exists(manifestPath));

        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var entry = manifest.RootElement.GetProperty("bundles").GetProperty("wwwroot/dist/site.min.js");
        Assert.IsTrue(entry.GetProperty("fingerprinted").GetBoolean());
        Assert.AreEqual("js", entry.GetProperty("type").GetString());
        Assert.IsFalse(Path.IsPathRooted(entry.GetProperty("output").GetString()!));
        Assert.IsFalse(entry.GetProperty("output").GetString()!.Contains('\\'));

        var fingerprintedFiles = Directory.GetFiles(Path.Combine(workspace.Root, "wwwroot/dist"), "site.min.*.js");
        Assert.HasCount(1, fingerprintedFiles);
        var fingerprintedContent = File.ReadAllText(fingerprintedFiles[0]);
        var expectedFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintedContent))).ToLowerInvariant()[..8];
        Assert.AreEqual($"wwwroot/dist/site.min.{expectedFingerprint}.js", entry.GetProperty("output").GetString());
    }

    [TestMethod]
    public void ValidateCommandDoesNotWriteOutputs()
    {
        using var workspace = new SampleWorkspace("AspNetMvcSample");
        var cli = new CliApplication();

        var exitCode = cli.Run(["validate", "--config", workspace.BundleConfigPath], new StringWriter(), new StringWriter());

        Assert.AreEqual(0, exitCode);
        Assert.IsFalse(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css")));
        Assert.IsFalse(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/site.min.js")));
    }

    [TestMethod]
    public void CheckCommandDoesNotWriteOutputs()
    {
        using var workspace = new SampleWorkspace("AspNetMvcSample");
        File.WriteAllText(
            workspace.BundleConfigPath,
            """
            {
              "$schema": "https://raw.githubusercontent.com/korgys/WebBundler/main/schemas/bundleconfig.v1.schema.json",
              "version": 1,
              "manifestOutput": "wwwroot/dist/webbundler.manifest.json",
              "bundles": [
                {
                  "output": "wwwroot/dist/site.min.css",
                  "inputs": [
                    "wwwroot/css/base.css",
                    "wwwroot/css/app.css"
                  ],
                  "type": "css",
                  "minify": true
                }
              ]
            }
            """);
        var cli = new CliApplication();

        var exitCode = cli.Run(["check", "--config", workspace.BundleConfigPath], new StringWriter(), new StringWriter());

        Assert.AreEqual(0, exitCode);
        Assert.IsFalse(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css")));
        Assert.IsFalse(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/site.min.js")));
        Assert.IsFalse(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/webbundler.manifest.json")));
    }

    [TestMethod]
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

        Assert.AreEqual(3, exitCode);
        Assert.IsFalse(File.Exists(Path.Combine(workspace.Root, "wwwroot/dist/site.min.css")));
    }

    [TestMethod]
    public void MissingConfigurationReturnsNonZeroExitCode()
    {
        var cli = new CliApplication();
        var exitCode = cli.Run(["build", "--config", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "bundleconfig.json")], new StringWriter(), new StringWriter());

        Assert.AreEqual(2, exitCode);
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
