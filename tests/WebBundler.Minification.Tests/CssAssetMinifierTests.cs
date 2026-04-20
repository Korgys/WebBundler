using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebBundler.Minification;

namespace WebBundler.Minification.Tests;

[TestClass]
public sealed class CssAssetMinifierTests
{
    [TestMethod]
    public void MinifiesCssByRemovingCommentsAndExtraWhitespace()
    {
        var minifier = new CssAssetMinifier();

        var result = minifier.Minify("""
            /* banner */
            body {
                margin : 0 ;
                color : red;
            }
            """);

        Assert.AreEqual("body{margin:0;color:red}", result);
    }

    [TestMethod]
    public void ReturnsEmptyStringForWhitespaceContent()
    {
        var minifier = new CssAssetMinifier();

        Assert.AreEqual(string.Empty, minifier.Minify("   \r\n\t"));
    }
}
