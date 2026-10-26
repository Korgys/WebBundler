using System.Text;
using WebBundler.Core;

namespace WebBundler.Minification;

public sealed class CssAssetMinifier : IAssetMinifier
{
    public BundleType SupportedType => BundleType.Css;

    public string Minify(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(content.Length);
        var quote = '\0';
        var inUrl = false;
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

            if (escaped)
            {
                builder.Append(current);
                escaped = false;
                continue;
            }

            if (current == '\\')
            {
                builder.Append(current);
                escaped = true;
                continue;
            }

            if (quote != '\0')
            {
                builder.Append(current);
                if (current == quote)
                {
                    quote = '\0';
                }

                continue;
            }

            if (current is '\'' or '"')
            {
                quote = current;
                builder.Append(current);
                continue;
            }

            if (inUrl)
            {
                builder.Append(current);
                if (current == ')')
                {
                    inUrl = false;
                }

                continue;
            }

            if (current == '/' && next == '*')
            {
                inComment = true;
                index++;
                continue;
            }

            if (current == '(' && index >= 3
                && content.AsSpan(index - 3, 3).Equals("url", StringComparison.OrdinalIgnoreCase)
                && (index == 3 || !(char.IsLetterOrDigit(content[index - 4]) || content[index - 4] is '_' or '-' or '\\')))
            {
                inUrl = true;
            }

            if (char.IsWhiteSpace(current))
            {
                if (builder.Length > 0 && builder[^1] != ' ' && !TrimsAdjacentWhitespace(builder[^1]))
                {
                    builder.Append(' ');
                }

                continue;
            }

            if (TrimsAdjacentWhitespace(current))
            {
                if (builder.Length > 0 && builder[^1] == ' ')
                {
                    builder.Length--;
                }

                if (current == '}' && builder.Length > 0 && builder[^1] == ';')
                {
                    builder.Length--;
                }
            }

            builder.Append(current);
        }

        return builder.ToString().Trim();
    }

    private static bool TrimsAdjacentWhitespace(char value)
    {
        return value is '{' or '}' or ':' or ';' or ',' or '>' or '~';
    }
}
