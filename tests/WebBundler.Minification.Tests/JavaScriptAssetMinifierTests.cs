using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebBundler.Minification;

namespace WebBundler.Minification.Tests;

[TestClass]
public sealed class JavaScriptAssetMinifierTests
{
    [TestMethod]
    public void MinifiesJavaScriptWhilePreservingStrings()
    {
        var minifier = new JavaScriptAssetMinifier();

        var result = minifier.Minify("""
            // banner
            const url = "https://example.com/app.js";
            const template = `literal // text`;
            /* remove me */
            function run() {
                return url + template; // trailing
            }
            """);

        Assert.AreEqual(
            "const url = \"https://example.com/app.js\"; const template = `literal // text`; function run() { return url + template; }",
            result);
    }

    [TestMethod]
    public void ReturnsEmptyStringForWhitespaceContent()
    {
        var minifier = new JavaScriptAssetMinifier();

        Assert.AreEqual(string.Empty, minifier.Minify("\r\n  \t"));
    }
}
