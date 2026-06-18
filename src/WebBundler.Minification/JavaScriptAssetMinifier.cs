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
        var inRegexLiteral = false;
        var inRegexCharacterClass = false;
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

            if (inRegexLiteral)
            {
                builder.Append(c);

                if (!escaped)
                {
                    if (c == '[')
                    {
                        inRegexCharacterClass = true;
                    }
                    else if (c == ']')
                    {
                        inRegexCharacterClass = false;
                    }
                    else if (c == '/' && !inRegexCharacterClass)
                    {
                        inRegexLiteral = false;
                    }
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

            if (c == '/' && CanStartRegexLiteral(builder))
            {
                inRegexLiteral = true;
                inRegexCharacterClass = false;
                escaped = false;
                builder.Append(c);
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

    private static bool CanStartRegexLiteral(StringBuilder builder)
    {
        var index = builder.Length - 1;
        while (index >= 0 && char.IsWhiteSpace(builder[index]))
        {
            index--;
        }

        if (index < 0)
        {
            return true;
        }

        if (builder[index] == '>' && index > 0 && builder[index - 1] == '=')
        {
            return true;
        }

        if ("=([{,:;!&|?+-*~%^<>".Contains(builder[index]))
        {
            return true;
        }

        return PreviousTokenIsRegexPrefixKeyword(builder, index);
    }

    private static bool PreviousTokenIsRegexPrefixKeyword(StringBuilder builder, int tokenEndIndex)
    {
        if (!IsIdentifierPart(builder[tokenEndIndex]))
        {
            return false;
        }

        var tokenStartIndex = tokenEndIndex;
        while (tokenStartIndex > 0 && IsIdentifierPart(builder[tokenStartIndex - 1]))
        {
            tokenStartIndex--;
        }

        var token = builder.ToString(tokenStartIndex, tokenEndIndex - tokenStartIndex + 1);

        return token is "return" or "throw" or "case" or "delete" or "typeof" or "void" or "new" or "in" or "of";
    }

    private static bool IsIdentifierPart(char c)
    {
        return char.IsLetterOrDigit(c) || c is '_' or '$';
    }
}
