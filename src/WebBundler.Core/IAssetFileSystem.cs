namespace WebBundler.Core;

public interface IAssetFileSystem
{
    bool FileExists(string path);

    bool DirectoryExists(string path);

    string GetFullPath(string path);

    string Combine(string left, string right);

    string GetDirectoryName(string path);

    string GetFileName(string path);

    IEnumerable<string> EnumerateFiles(string directory, bool recursive = false);

    string ReadAllText(string path);

    void WriteAllText(string path, string content);

    void CreateDirectory(string path);
}
