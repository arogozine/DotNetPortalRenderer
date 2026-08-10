// AI Assisted
using System.Text;

namespace DoomAssetLoader.Decorate
{
    public static class DecorateLexer
    {
        public static List<DecorateToken> Tokenize(ReadOnlySpan<char> str)
        {
            List<DecorateToken> tokens = [];

            for (int i = 0; i < str.Length; )
            {
                char c = str[i];

                if (c == '\r')
                {
                    i++;
                    continue;
                }

                if (c == '\n')
                {
                    tokens.Add(DecorateToken.EndOfLine);
                    i++;
                    continue;
                }

                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                if (c == '/' && i + 1 < str.Length && str[i + 1] == '/')
                {
                    SkipToEndOfLine(ref i, str);
                    continue;
                }

                if (c == '/' && i + 1 < str.Length && str[i + 1] == '*')
                {
                    SkipBlockComment(ref i, str);
                    continue;
                }

                if (c == '"')
                {
                    tokens.Add(ReadString(ref i, str));
                    continue;
                }

                if (c == ':' && i + 1 < str.Length && str[i + 1] == ':')
                {
                    tokens.Add(new DecorateToken(DecorateTokenType.Symbol, "::"));
                    i += 2;
                    continue;
                }

                if (IsSymbol(c))
                {
                    tokens.Add(new DecorateToken(DecorateTokenType.Symbol, c.ToString()));
                    i++;
                    continue;
                }

                tokens.Add(ReadWord(ref i, str));
            }

            tokens.Add(DecorateToken.EndOfFile);
            return tokens;
        }

        private static bool IsSymbol(char c) => c is '{' or '}' or '(' or ')' or ',' or ':' or ';' or '=' or '+' or '-';

        private static void SkipToEndOfLine(ref int i, ReadOnlySpan<char> str)
        {
            for (; i < str.Length && str[i] != '\n'; i++)
                ;
        }

        private static void SkipBlockComment(ref int i, ReadOnlySpan<char> str)
        {
            i += 2;

            for (; i < str.Length; i++)
            {
                if (str[i] == '*' && i + 1 < str.Length && str[i + 1] == '/')
                {
                    i += 2;
                    return;
                }
            }
        }

        private static DecorateToken ReadWord(ref int i, ReadOnlySpan<char> str)
        {
            int start = i;

            for (; i < str.Length; i++)
            {
                char c = str[i];

                if (char.IsWhiteSpace(c) || IsSymbol(c) || c == '"')
                {
                    break;
                }

                if (c == '/' && i + 1 < str.Length && (str[i + 1] == '/' || str[i + 1] == '*'))
                {
                    break;
                }
            }

            return new DecorateToken(DecorateTokenType.Word, new string(str[start..i]));
        }

        private static DecorateToken ReadString(ref int i, ReadOnlySpan<char> str)
        {
            i++;

            StringBuilder builder = new();

            while (i < str.Length)
            {
                char c = str[i];

                if (c == '\\' && i + 1 < str.Length && str[i + 1] == '"')
                {
                    _ = builder.Append('"');
                    i += 2;
                    continue;
                }

                if (c == '"')
                {
                    i++;
                    break;
                }

                _ = builder.Append(c);
                i++;
            }

            return new DecorateToken(DecorateTokenType.String, builder.ToString());
        }
    }
}
