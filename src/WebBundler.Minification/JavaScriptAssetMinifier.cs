using System.Text;
using WebBundler.Core;

namespace WebBundler.Minification;

public sealed class JavaScriptAssetMinifier : IAssetMinifier
{
    public BundleType SupportedType => BundleType.JavaScript;

    public string Minify(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(content.Length);
        var inSingleQuote = false;
        var inDoubleQuote = false;
        var inTemplate = false;
        var inLineComment = false;
        var inBlockComment = false;
        var escaped = false;

        for (var index = 0; index < content.Length; index++)
        {
            var c = content[index];
            var next = index + 1 < content.Length ? content[index + 1] : '\0';

            if (inLineComment)
            {
                if (c is '\n' or '\r')
                {
                    inLineComment = false;
                    if (builder.Length > 0 && builder[^1] != ' ')
                    {
                        builder.Append(' ');
                    }
                }

                continue;
            }

            if (inBlockComment)
            {
                if (c == '*' && next == '/')
                {
                    inBlockComment = false;
                    index++;
                    if (builder.Length > 0 && builder[^1] != ' ')
                    {
                        builder.Append(' ');
                    }
                }

                continue;
            }

            if (inSingleQuote)
            {
                builder.Append(c);
                if (!escaped && c == '\'')
                {
                    inSingleQuote = false;
                }

                escaped = !escaped && c == '\\';
                continue;
            }

            if (inDoubleQuote)
            {
                builder.Append(c);
                if (!escaped && c == '"')
                {
                    inDoubleQuote = false;
                }

                escaped = !escaped && c == '\\';
                continue;
            }

            if (inTemplate)
            {
                builder.Append(c);
                if (!escaped && c == '`')
                {
                    inTemplate = false;
                }

                escaped = !escaped && c == '\\';
                continue;
            }

            if (c == '/' && next == '/')
            {
                inLineComment = true;
                index++;
                continue;
            }

            if (c == '/' && next == '*')
            {
                inBlockComment = true;
                index++;
                continue;
            }

            if (c == '\'')
            {
                inSingleQuote = true;
                builder.Append(c);
                escaped = false;
                continue;
            }

            if (c == '"')
            {
                inDoubleQuote = true;
                builder.Append(c);
                escaped = false;
                continue;
            }

            if (c == '`')
            {
                inTemplate = true;
                builder.Append(c);
                escaped = false;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (builder.Length > 0 && builder[^1] != ' ')
                {
                    builder.Append(' ');
                }

                continue;
            }

            builder.Append(c);
        }

        return builder.ToString().Trim();
    }
}
