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

    [TestMethod]
    public void MinifiesRealWorldCssWithoutBreakingMediaQueries()
    {
        var minifier = new CssAssetMinifier();

        var result = minifier.Minify("""
            /* app shell */
            @media screen and (min-width: 800px) {
                .hero,
                .hero--featured {
                    background-image: url("/images/hero.png");
                    padding: 0 16px;
                }
            }
            """);

        Assert.AreEqual(
            "@media screen and (min-width:800px){.hero,.hero--featured{background-image:url(\"/images/hero.png\");padding:0 16px}}",
            result);
    }
}
