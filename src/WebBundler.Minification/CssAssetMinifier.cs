using System.Text.RegularExpressions;
using WebBundler.Core;

namespace WebBundler.Minification;

public sealed class CssAssetMinifier : IAssetMinifier
{
    private static readonly Regex BlockComments = new(@"/\*.*?\*/", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex TrimSpacesAroundTokens = new(@"\s*([{}:;,>+~])\s*", RegexOptions.Compiled);

    public BundleType SupportedType => BundleType.Css;

    public string Minify(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var minified = BlockComments.Replace(content, string.Empty);
        minified = Whitespace.Replace(minified, " ");
        minified = TrimSpacesAroundTokens.Replace(minified, "$1");
        minified = minified.Replace(";}", "}");
        return minified.Trim();
    }
}
