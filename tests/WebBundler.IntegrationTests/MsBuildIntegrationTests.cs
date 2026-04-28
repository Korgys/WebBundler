using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WebBundler.IntegrationTests;

[TestClass]
public sealed class MsBuildIntegrationTests
{
  private static readonly Lazy<PackageFeedInfo> PackageFeed = new(CreatePackageFeed, isThreadSafe: true);

  [TestMethod]
  public void BuildWritesOutputsByDefault()
  {
    using var workspace = new MsBuildWorkspace();
    workspace.WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="WebBundler.MSBuild" Version="__WEBBUNDLER_PACKAGE_VERSION__" PrivateAssets="all" />
              </ItemGroup>
            </Project>
            """);
    workspace.WriteFile("Program.cs", "Console.WriteLine(\"Hello\");");
    workspace.WriteFile("bundleconfig.json", """
            {
              "version": 1,
              "bundles": [
                {
                  "output": "wwwroot/dist/site.min.css",
                  "inputs": [ "wwwroot/css/reset.css", "wwwroot/css/site.css" ],
                  "type": "css",
                  "minify": true,
                  "sourceMap": true
                }
              ]
            }
            """);
    workspace.WriteFile("wwwroot/css/reset.css", "body { margin: 0; }\n");
    workspace.WriteFile("wwwroot/css/site.css", "h1 { color: red; }\n");

    var result = workspace.RunDotNet(["build", "-c", "Release", "--nologo", "-v:minimal"]);

    Assert.AreEqual(0, result.ExitCode, result.Output);
    Assert.IsTrue(File.Exists(workspace.ProjectPath("wwwroot/dist/site.min.css")));
    Assert.IsTrue(File.Exists(workspace.ProjectPath("wwwroot/dist/site.min.css.map")));
    StringAssert.Contains(File.ReadAllText(workspace.ProjectPath("wwwroot/dist/site.min.css")), "sourceMappingURL=site.min.css.map");
    Assert.AreEqual(1, CountOccurrences(result.Output, "Built 'wwwroot/dist/site.min.css' from 2 file(s)."));
  }

  [TestMethod]
  public void PublishRunsBundlingOnce()
  {
    using var workspace = new MsBuildWorkspace();
    workspace.WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="WebBundler.MSBuild" Version="__WEBBUNDLER_PACKAGE_VERSION__" PrivateAssets="all" />
              </ItemGroup>
            </Project>
            """);
    workspace.WriteFile("Program.cs", "Console.WriteLine(\"Hello\");");
    workspace.WriteFile("bundleconfig.json", """
            {
              "version": 1,
              "bundles": [
                {
                  "output": "wwwroot/dist/site.min.js",
                  "inputs": [ "wwwroot/js/a.js", "wwwroot/js/b.js" ],
                  "type": "javascript",
                  "minify": false
                }
              ]
            }
            """);
    workspace.WriteFile("wwwroot/js/a.js", "window.a = 1;\n");
    workspace.WriteFile("wwwroot/js/b.js", "window.b = 2;\n");

    var result = workspace.RunDotNet(["publish", "-c", "Release", "--nologo", "-v:minimal"]);

    Assert.AreEqual(0, result.ExitCode, result.Output);
    Assert.IsTrue(File.Exists(workspace.ProjectPath("wwwroot/dist/site.min.js")));
    Assert.AreEqual(1, CountOccurrences(result.Output, "Built 'wwwroot/dist/site.min.js' from 2 file(s)."));
  }

  [TestMethod]
  public void WriteOutputsFalseValidatesWithoutWritingFiles()
  {
    using var workspace = new MsBuildWorkspace();
    workspace.WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <WebBundlerWriteOutputs>false</WebBundlerWriteOutputs>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="WebBundler.MSBuild" Version="__WEBBUNDLER_PACKAGE_VERSION__" PrivateAssets="all" />
              </ItemGroup>
            </Project>
            """);
    workspace.WriteFile("Program.cs", "Console.WriteLine(\"Hello\");");
    workspace.WriteFile("bundleconfig.json", """
            {
              "version": 1,
              "bundles": [
                {
                  "output": "wwwroot/dist/site.min.css",
                  "inputs": [ "wwwroot/css/site.css" ],
                  "type": "css",
                  "minify": false,
                  "sourceMap": true
                }
              ]
            }
            """);
    workspace.WriteFile("wwwroot/css/site.css", "body { color: black; }\n");

    var result = workspace.RunDotNet(["build", "-c", "Release", "--nologo", "-v:minimal"]);

    Assert.AreEqual(0, result.ExitCode, result.Output);
    Assert.IsFalse(File.Exists(workspace.ProjectPath("wwwroot/dist/site.min.css")));
    Assert.IsFalse(File.Exists(workspace.ProjectPath("wwwroot/dist/site.min.css.map")));
    Assert.AreEqual(1, CountOccurrences(result.Output, "Built 'wwwroot/dist/site.min.css' from 1 file(s)."));
  }

  [TestMethod]
  public void DisabledTargetSkipsExecution()
  {
    using var workspace = new MsBuildWorkspace();
    workspace.WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <WebBundlerEnabled>false</WebBundlerEnabled>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="WebBundler.MSBuild" Version="__WEBBUNDLER_PACKAGE_VERSION__" PrivateAssets="all" />
              </ItemGroup>
            </Project>
            """);
    workspace.WriteFile("Program.cs", "Console.WriteLine(\"Hello\");");
    workspace.WriteFile("bundleconfig.json", """
            {
              "version": 1,
              "bundles": [
                {
                  "output": "wwwroot/dist/site.min.css",
                  "inputs": [ "wwwroot/css/site.css" ],
                  "type": "css",
                  "minify": false
                }
              ]
            }
            """);
    workspace.WriteFile("wwwroot/css/site.css", "body { color: black; }\n");

    var result = workspace.RunDotNet(["build", "-c", "Release", "--nologo", "-v:minimal"]);

    Assert.AreEqual(0, result.ExitCode, result.Output);
    Assert.IsFalse(File.Exists(workspace.ProjectPath("wwwroot/dist/site.min.css")));
    Assert.AreEqual(0, CountOccurrences(result.Output, "Built 'wwwroot/dist/site.min.css' from 1 file(s)."));
  }

  [TestMethod]
  public void CustomConfigFileIsHonored()
  {
    using var workspace = new MsBuildWorkspace();
    workspace.WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <WebBundlerConfigFile>$(MSBuildProjectDirectory)/configs/custom.bundleconfig.json</WebBundlerConfigFile>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="WebBundler.MSBuild" Version="__WEBBUNDLER_PACKAGE_VERSION__" PrivateAssets="all" />
              </ItemGroup>
            </Project>
            """);
    workspace.WriteFile("Program.cs", "Console.WriteLine(\"Hello\");");
    workspace.WriteFile("configs/custom.bundleconfig.json", """
            {
              "version": 1,
              "bundles": [
                {
                  "output": "wwwroot/dist/custom.min.js",
                  "inputs": [ "wwwroot/js/site.js" ],
                  "type": "javascript",
                  "minify": false
                }
              ]
            }
            """);
    workspace.WriteFile("wwwroot/js/site.js", "window.site = true;\n");

    var result = workspace.RunDotNet(["build", "-c", "Release", "--nologo", "-v:minimal"]);

    Assert.AreEqual(0, result.ExitCode, result.Output);
    Assert.IsTrue(File.Exists(workspace.ProjectPath("wwwroot/dist/custom.min.js")));
    Assert.AreEqual(1, CountOccurrences(result.Output, "Built 'wwwroot/dist/custom.min.js' from 1 file(s)."));
  }

  [TestMethod]
  public void FingerprintingCanBeEnabledEndToEnd()
  {
    using var workspace = new MsBuildWorkspace();
    workspace.WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <WebBundlerEnableFingerprinting>true</WebBundlerEnableFingerprinting>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="WebBundler.MSBuild" Version="__WEBBUNDLER_PACKAGE_VERSION__" PrivateAssets="all" />
              </ItemGroup>
            </Project>
            """);
    workspace.WriteFile("Program.cs", "Console.WriteLine(\"Hello\");");
    workspace.WriteFile("bundleconfig.json", """
            {
              "version": 1,
              "bundles": [
                {
                  "output": "wwwroot/dist/site.min.js",
                  "inputs": [ "wwwroot/js/site.js" ],
                  "type": "javascript",
                  "minify": false,
                  "fingerprint": true,
                  "sourceMap": true
                }
              ]
            }
            """);
    workspace.WriteFile("wwwroot/js/site.js", "window.site = true;\n");

    var result = workspace.RunDotNet(["build", "-c", "Release", "--nologo", "-v:minimal"]);

    Assert.AreEqual(0, result.ExitCode, result.Output);
    var fingerprintedFiles = Directory.GetFiles(workspace.ProjectPath("wwwroot/dist"), "site.min.*.js");
    Assert.HasCount(1, fingerprintedFiles);
    var fingerprintedContent = File.ReadAllText(fingerprintedFiles[0]);
    var expectedFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintedContent))).ToLowerInvariant()[..8];
    Assert.AreEqual($"site.min.{expectedFingerprint}.js", Path.GetFileName(fingerprintedFiles[0]));
    Assert.IsFalse(File.Exists(workspace.ProjectPath("wwwroot/dist/site.min.js")));
    Assert.IsTrue(File.Exists(workspace.ProjectPath("wwwroot/dist/site.min.js.map")));
    StringAssert.Contains(fingerprintedContent, "sourceMappingURL=site.min.js.map");
  }

  [TestMethod]
  public void ManifestCanBeWrittenWithoutFingerprinting()
  {
    using var workspace = new MsBuildWorkspace();
    workspace.WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="WebBundler.MSBuild" Version="__WEBBUNDLER_PACKAGE_VERSION__" PrivateAssets="all" />
              </ItemGroup>
            </Project>
            """);
    workspace.WriteFile("Program.cs", "Console.WriteLine(\"Hello\");");
    workspace.WriteFile("bundleconfig.json", """
            {
              "version": 1,
              "manifestOutput": "wwwroot/dist/webbundler.manifest.json",
              "bundles": [
                {
                  "output": "wwwroot/dist/site.min.css",
                  "inputs": [ "wwwroot/css/site.css" ],
                  "type": "css",
                  "minify": false
                }
              ]
            }
            """);
    workspace.WriteFile("wwwroot/css/site.css", "body { color: black; }\n");

    var result = workspace.RunDotNet(["build", "-c", "Release", "--nologo", "-v:minimal"]);

    Assert.AreEqual(0, result.ExitCode, result.Output);
    var manifestPath = workspace.ProjectPath("wwwroot/dist/webbundler.manifest.json");
    Assert.IsTrue(File.Exists(manifestPath));

    using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
    var entry = manifest.RootElement.GetProperty("bundles").GetProperty("wwwroot/dist/site.min.css");
    Assert.IsFalse(entry.GetProperty("fingerprinted").GetBoolean());
    Assert.AreEqual("css", entry.GetProperty("type").GetString());
    Assert.AreEqual("wwwroot/dist/site.min.css", entry.GetProperty("output").GetString());
    Assert.IsFalse(Path.IsPathRooted(entry.GetProperty("output").GetString()!));
    Assert.IsFalse(entry.GetProperty("output").GetString()!.Contains('\\'));
  }

  [TestMethod]
  public void BuildFailsWhenInputsAreMissing()
  {
    using var workspace = new MsBuildWorkspace();
    workspace.WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="WebBundler.MSBuild" Version="__WEBBUNDLER_PACKAGE_VERSION__" PrivateAssets="all" />
              </ItemGroup>
            </Project>
            """);
    workspace.WriteFile("Program.cs", "Console.WriteLine(\"Hello\");");
    workspace.WriteFile("bundleconfig.json", """
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

    var result = workspace.RunDotNet(["build", "-c", "Release", "--nologo", "-v:minimal"]);

    Assert.AreNotEqual(0, result.ExitCode, result.Output);
    StringAssert.Contains(result.Output, "did not match any files");
    Assert.IsFalse(File.Exists(workspace.ProjectPath("wwwroot/dist/site.min.css")));
  }

  [TestMethod]
  public void BuildFailsForDuplicateOutputsAfterNormalization()
  {
    using var workspace = new MsBuildWorkspace();
    workspace.WriteProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <WebBundlerWriteOutputs>false</WebBundlerWriteOutputs>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="WebBundler.MSBuild" Version="__WEBBUNDLER_PACKAGE_VERSION__" PrivateAssets="all" />
              </ItemGroup>
            </Project>
            """);
    workspace.WriteFile("Program.cs", "Console.WriteLine(\"Hello\");");
    workspace.WriteFile("bundleconfig.json", """
            {
              "version": 1,
              "bundles": [
                {
                  "output": "wwwroot/dist/site.min.css",
                  "inputs": [ "wwwroot/css/site.css" ],
                  "type": "css",
                  "minify": false
                },
                {
                  "output": "wwwroot/dist/../dist/site.min.css",
                  "inputs": [ "wwwroot/css/theme.css" ],
                  "type": "css",
                  "minify": false
                }
              ]
            }
            """);
    workspace.WriteFile("wwwroot/css/site.css", "body { color: black; }\n");
    workspace.WriteFile("wwwroot/css/theme.css", "body { color: blue; }\n");

    var result = workspace.RunDotNet(["build", "-c", "Release", "--nologo", "-v:minimal"]);

    Assert.AreNotEqual(0, result.ExitCode, result.Output);
    StringAssert.Contains(result.Output, "Multiple bundles resolve to the same output path");
    Assert.IsFalse(File.Exists(workspace.ProjectPath("wwwroot/dist/site.min.css")));
  }

  private static int CountOccurrences(string haystack, string needle)
  {
    var count = 0;
    var index = 0;
    while (true)
    {
      index = haystack.IndexOf(needle, index, StringComparison.Ordinal);
      if (index < 0)
      {
        return count;
      }

      count++;
      index += needle.Length;
    }
  }

  private static PackageFeedInfo CreatePackageFeed()
  {
    var feed = Path.Combine(System.IO.Path.GetTempPath(), "WebBundler.Tests", "packages", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(feed);
    var version = $"1.0.0-msbuildtest.{Guid.NewGuid():N}".ToLowerInvariant();

    var repoRoot = FindRepositoryRoot();
    var projects = new[]
    {
            Path.Combine(repoRoot, "src", "WebBundler.Core", "WebBundler.Core.csproj"),
            Path.Combine(repoRoot, "src", "WebBundler.Configuration", "WebBundler.Configuration.csproj"),
            Path.Combine(repoRoot, "src", "WebBundler.Fingerprinting", "WebBundler.Fingerprinting.csproj"),
            Path.Combine(repoRoot, "src", "WebBundler.Minification", "WebBundler.Minification.csproj"),
            Path.Combine(repoRoot, "src", "WebBundler.MSBuild", "WebBundler.MSBuild.csproj")
        };

    foreach (var project in projects)
    {
      RunProcess(
          "dotnet",
          [$"pack", project, "-c", "Release", "-o", feed, "-p:PackageVersion=" + version, "--nologo", "-v:minimal"],
          repoRoot);
    }

    return new PackageFeedInfo(feed, version);
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

  private static ProcessResult RunProcess(string fileName, IReadOnlyList<string> arguments, string workingDirectory)
  {
    var startInfo = new ProcessStartInfo(fileName)
    {
      WorkingDirectory = workingDirectory,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };

    foreach (var argument in arguments)
    {
      startInfo.ArgumentList.Add(argument);
    }

    using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start '{fileName}'.");
    var output = process.StandardOutput.ReadToEnd();
    var error = process.StandardError.ReadToEnd();
    process.WaitForExit();

    return new ProcessResult(process.ExitCode, output + error);
  }

  private sealed class MsBuildWorkspace : IDisposable
  {
    public MsBuildWorkspace()
    {
      Root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WebBundler.Tests", Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(Root);
      Directory.CreateDirectory(System.IO.Path.Combine(Root, "configs"));
      Directory.CreateDirectory(System.IO.Path.Combine(Root, "wwwroot"));
    }

    public string Root { get; }

    public string ProjectPath(string relativePath) => System.IO.Path.Combine(Root, relativePath);

    public void WriteProject(string content) =>
        File.WriteAllText(
            ProjectPath("WebBundler.Sample.csproj"),
            content.Replace("__WEBBUNDLER_PACKAGE_VERSION__", PackageFeed.Value.Version, StringComparison.Ordinal));

    public void WriteFile(string relativePath, string content)
    {
      var path = ProjectPath(relativePath);
      Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
      File.WriteAllText(path, content);
    }

    public ProcessResult RunDotNet(IReadOnlyList<string> arguments)
    {
      var finalArguments = new List<string>(arguments.Count + 2);
      finalArguments.Add(arguments[0]);
      finalArguments.Add(ProjectPath("WebBundler.Sample.csproj"));
      finalArguments.AddRange(arguments.Skip(1));
      finalArguments.Add("-p:RestoreAdditionalProjectSources=" + PackageFeed.Value.FeedPath);

      return RunProcess("dotnet", finalArguments, Root);
    }

    public void Dispose()
    {
      if (Directory.Exists(Root))
      {
        Directory.Delete(Root, recursive: true);
      }
    }
  }

  private sealed record PackageFeedInfo(string FeedPath, string Version);

  private sealed record ProcessResult(int ExitCode, string Output);
}
