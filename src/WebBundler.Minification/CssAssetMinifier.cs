using System.Text;
using System.Text.RegularExpressions;
using WebBundler.Core;

namespace WebBundler.Minification;

public sealed class CssAssetMinifier : IAssetMinifier
{
    private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(2);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled, RegexMatchTimeout);
    private static readonly Regex TrimSpacesAroundTokens = new(@"\s*([{}:;,>+~])\s*", RegexOptions.Compiled, RegexMatchTimeout);

    public BundleType SupportedType => BundleType.Css;

    public string Minify(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var minified = RemoveCommentsOutsideStrings(content);
        minified = Whitespace.Replace(minified, " ");
        minified = TrimSpacesAroundTokens.Replace(minified, "$1");
        minified = minified.Replace(";}", "}");
        return minified.Trim();
    }

    private static string RemoveCommentsOutsideStrings(string content)
    {
        var builder = new StringBuilder(content.Length);
        var inSingleQuote = false;
        var inDoubleQuote = false;
        var inComment = false;
        var escaped = false;

        for (var index = 0; index < content.Length; index++)
        {
            var current = content[index];
            var next = index + 1 < content.Length ? content[index + 1] : '\0';

            if (inComment)
            {
                if (current == '*' && next == '/')
                {
                    inComment = false;
                    index++;
                }

                continue;
            }

            if (!inSingleQuote && !inDoubleQuote && current == '/' && next == '*')
            {
                inComment = true;
                index++;
                continue;
            }

            builder.Append(current);

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if ((inSingleQuote || inDoubleQuote) && current == '\\')
            {
                escaped = true;
                continue;
            }

            if (!inDoubleQuote && current == '\'')
            {
                inSingleQuote = !inSingleQuote;
                continue;
            }

            if (!inSingleQuote && current == '"')
            {
                inDoubleQuote = !inDoubleQuote;
            }
        }

        return builder.ToString();
    }
}
