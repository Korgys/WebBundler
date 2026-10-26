namespace WebBundler.Minification.Tests;

[TestClass]
public sealed class CssAssetMinifierTests
{
    [TestMethod]
    [DataRow("calc(100% + 2px)")]
    [DataRow("calc(100% - 2px)")]
    [DataRow("calc((100% + 2px) / 2)")]
    [DataRow("clamp(1px, calc(2px + 3vw), 100px)")]
    public void PreservesMathOperatorSpacing(string value)
    {
        var minifier = new CssAssetMinifier();

        var result = minifier.Minify(".box { width: " + value + "; }");

        Assert.AreEqual(".box{width:" + value.Replace(", ", ",") + "}", result);
    }

    [TestMethod]
    [DataRow("\"a  +  b : c ; } > ~ , /* text */\"")]
    [DataRow("'a  +  b : c ; } > ~ , /* text */'")]
    [DataRow("\"escaped \\\" quote  +  /* text */\"")]
    [DataRow("'escaped \\' quote  +  /* text */'")]
    [DataRow("\"trailing backslash \\\\\"")]
    public void PreservesStringContentsExactly(string value)
    {
        var minifier = new CssAssetMinifier();

        var result = minifier.Minify(".box { content: " + value + "; /* remove */ color: red; }");

        Assert.AreEqual(".box{content:" + value + ";color:red}", result);
    }

    [TestMethod]
    [DataRow("url(\"data:image/svg+xml;utf8,<svg>  +  ;}</svg>\")")]
    [DataRow("URL('data:image/svg+xml;utf8,<svg>  +  ;}</svg>')")]
    [DataRow("url(data:image/svg+xml;utf8,<svg>/*text*/+;}</svg>)")]
    [DataRow("url(images/icon\\)name.svg)")]
    public void PreservesUrlContentsExactly(string value)
    {
        var minifier = new CssAssetMinifier();

        var result = minifier.Minify(".box { background: " + value + "; /* remove */ color: red; }");

        Assert.AreEqual(".box{background:" + value + ";color:red}", result);
    }

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

    [TestMethod]
    public void PreservesCommentMarkersInsideStringContent()
    {
        var minifier = new CssAssetMinifier();

        var result = minifier.Minify("""
            .icon {
                content: "/* not comment */";
            }
            """);

        Assert.AreEqual(".icon{content:\"/* not comment */\"}", result);
    }

    [TestMethod]
    public void PreservesCommentMarkersInsideDataUri()
    {
        var minifier = new CssAssetMinifier();

        var result = minifier.Minify("""
            .icon {
                background: url("data:image/svg+xml;<svg><style>/*...*/</style></svg>");
            }
            """);

        Assert.AreEqual(
            ".icon{background:url(\"data:image/svg+xml;<svg><style>/*...*/</style></svg>\")}",
            result);
    }
}
