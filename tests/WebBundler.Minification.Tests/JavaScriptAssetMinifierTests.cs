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

    [TestMethod]
    public void MinifiesRealWorldJavaScriptWhilePreservingStringsAndTemplates()
    {
        var minifier = new JavaScriptAssetMinifier();

        var result = minifier.Minify("""
            // app shell
            const apiBase = "https://example.com/api";
            const template = `\n                <span class="badge">${apiBase}</span>\n            `;
            function render() {
                /* remove me */
                return apiBase + template;
            }
            """);

        Assert.AreEqual(
            "const apiBase = \"https://example.com/api\"; const template = `\\n                <span class=\"badge\">${apiBase}</span>\\n            `; function render() { return apiBase + template; }",
            result);
    }

    [TestMethod]
    public void MinifiesJavaScriptWhilePreservingRegexLiteralUrls()
    {
        var minifier = new JavaScriptAssetMinifier();

        var result = minifier.Minify("""
            const r = /https?:\/\/example\.com/;
            const ok = r.test(value); // trailing
            """);

        Assert.AreEqual("const r = /https?:\\/\\/example\\.com/; const ok = r.test(value);", result);
    }

    [TestMethod]
    public void MinifiesJavaScriptWhilePreservingRegexLiteralAfterReturn()
    {
        var minifier = new JavaScriptAssetMinifier();

        var result = minifier.Minify("""
            function matches(value) {
                return /abc/.test(value); // trailing
            }
            """);

        Assert.AreEqual("function matches(value) { return /abc/.test(value); }", result);
    }
}
