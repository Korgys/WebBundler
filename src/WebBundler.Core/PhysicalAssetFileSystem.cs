using System.Text;

namespace WebBundler.Core;

public sealed class PhysicalAssetFileSystem : IAssetFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public string GetFullPath(string path) => Path.GetFullPath(path);

    public string Combine(string left, string right) => Path.Combine(left, right);

    public string GetDirectoryName(string path) =>
        Path.GetDirectoryName(path) ?? string.Empty;

    public string GetFileName(string path) => Path.GetFileName(path);

    public IEnumerable<string> EnumerateFiles(string directory, bool recursive = false) =>
        Directory.EnumerateFiles(
            directory,
            "*",
            recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public void WriteAllText(string path, string content) =>
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

    public void CreateDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        Directory.CreateDirectory(path);
    }
}
