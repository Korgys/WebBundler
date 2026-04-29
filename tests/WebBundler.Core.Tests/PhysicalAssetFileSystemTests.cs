using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebBundler.Core;

namespace WebBundler.Core.Tests;

[TestClass]
public sealed class PhysicalAssetFileSystemTests
{
    [TestMethod]
    public void WriteAllTextWritesUtf8WithoutBom()
    {
        using var workspace = new TestWorkspace();
        var fileSystem = new PhysicalAssetFileSystem();
        var path = Path.Combine(workspace.Root, "site.css");

        fileSystem.WriteAllText(path, "body { color: red; }");

        var bytes = File.ReadAllBytes(path);
        CollectionAssert.AreNotEqual(Encoding.UTF8.GetPreamble(), bytes.Take(3).ToArray());
        Assert.AreEqual("body { color: red; }", fileSystem.ReadAllText(path));
    }

    [TestMethod]
    public void CreateDirectoryIgnoresBlankPaths()
    {
        var fileSystem = new PhysicalAssetFileSystem();

        fileSystem.CreateDirectory(string.Empty);
        fileSystem.CreateDirectory(" ");
    }

    [TestMethod]
    public void EnumerateFilesHonorsRecursiveFlag()
    {
        using var workspace = new TestWorkspace();
        File.WriteAllText(Path.Combine(workspace.Root, "root.txt"), "root");
        Directory.CreateDirectory(Path.Combine(workspace.Root, "nested"));
        File.WriteAllText(Path.Combine(workspace.Root, "nested", "child.txt"), "child");
        var fileSystem = new PhysicalAssetFileSystem();

        var topLevel = fileSystem.EnumerateFiles(workspace.Root).Select(Path.GetFileName).Order().ToArray();
        var recursive = fileSystem.EnumerateFiles(workspace.Root, recursive: true).Select(Path.GetFileName).Order().ToArray();

        CollectionAssert.AreEqual(new[] { "root.txt" }, topLevel);
        CollectionAssert.AreEqual(new[] { "child.txt", "root.txt" }, recursive);
    }

    [TestMethod]
    public void PathHelpersReturnExpectedValues()
    {
        var fileSystem = new PhysicalAssetFileSystem();
        var combined = fileSystem.Combine("dist", "site.css");

        Assert.AreEqual(Path.Combine("dist", "site.css"), combined);
        Assert.AreEqual("site.css", fileSystem.GetFileName(combined));
        Assert.AreEqual(Path.GetDirectoryName(combined), fileSystem.GetDirectoryName(combined));
        Assert.AreEqual(Path.GetFullPath(combined), fileSystem.GetFullPath(combined));
    }

    [TestMethod]
    public void ExistenceChecksReflectFileSystemState()
    {
        using var workspace = new TestWorkspace();
        var fileSystem = new PhysicalAssetFileSystem();
        var path = Path.Combine(workspace.Root, "site.css");

        File.WriteAllText(path, "body{}");

        Assert.IsTrue(fileSystem.DirectoryExists(workspace.Root));
        Assert.IsTrue(fileSystem.FileExists(path));
        Assert.IsFalse(fileSystem.FileExists(Path.Combine(workspace.Root, "missing.css")));
    }

    private sealed class TestWorkspace : IDisposable
    {
        public TestWorkspace()
        {
            Root = Path.Combine(Path.GetTempPath(), "WebBundler.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
