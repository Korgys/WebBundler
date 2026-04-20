using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebBundler.Fingerprinting;

namespace WebBundler.Fingerprinting.Tests;

[TestClass]
public sealed class Sha256AssetFingerprinterTests
{
    [TestMethod]
    public void FingerprintsPathsInsideDirectories()
    {
        var fingerprinter = new Sha256AssetFingerprinter();

        var result = fingerprinter.Fingerprint(Path.Combine("wwwroot", "dist", "site.css"), "body { color: red; }");
        var expectedFingerprint = ComputeFingerprint("body { color: red; }");

        Assert.AreEqual(Path.Combine("wwwroot", "dist", "site.css"), result.OriginalPath);
        Assert.AreEqual(expectedFingerprint, result.Value);
        Assert.AreEqual(Path.Combine("wwwroot", "dist", $"site.{expectedFingerprint}.css"), result.FingerprintedPath);
    }

    [TestMethod]
    public void FingerprintsPathsWithoutDirectory()
    {
        var fingerprinter = new Sha256AssetFingerprinter();

        var result = fingerprinter.Fingerprint("site.js", "console.log(1);");
        var expectedFingerprint = ComputeFingerprint("console.log(1);");

        Assert.AreEqual("site.js", result.OriginalPath);
        Assert.AreEqual(expectedFingerprint, result.Value);
        Assert.AreEqual($"site.{expectedFingerprint}.js", result.FingerprintedPath);
    }

    private static string ComputeFingerprint(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant()[..8];
}
