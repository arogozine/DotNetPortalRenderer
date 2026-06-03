using System.Collections.Frozen;
using System.Diagnostics;

namespace BuildAssetLoader.Con
{
    public static class ConParser
    {
        public static List<ConToken> Parse(ReadOnlySpan<char> str)
        {
            List<ConToken> tokens = [];

            var commandList = Enum.GetValues<CommandList>()
                .ToFrozenDictionary(x => x.ToString(), x => x);

            var test = commandList.GetAlternateLookup<ReadOnlySpan<char>>();

            for (int i = 0; i < str.Length; i++)
            {
                SkipStartingSpace(ref i, str);

                if (i >= str.Length)
                {
                    break;
                }

                ReadOnlySpan<char> word = ReadNextWord(ref i, str);

                if (i >= str.Length)
                {
                    break;
                }

                if (word.StartsWith("\r\n"))
                {
                    tokens.Add(ConToken.NewLine);
                    continue;
                }

                if (word.StartsWith("//"))
                {
                    SkipToNextLine(ref i, str);
                    tokens.Add(ConToken.NewLine);

                    continue;
                }

                if (word.StartsWith("/*"))
                {
                    bool newLine = SkipCommentBlock(ref i, str);

                    if (newLine)
                    {
                        tokens.Add(ConToken.NewLine);
                    }

                    continue;
                }

                if (word.SequenceEqual("{"))
                {
                    tokens.Add(ConToken.BlockStart);
                    continue;
                }

                if (word.SequenceEqual("}"))
                {
                    tokens.Add(ConToken.BlockEnd);
                    continue;
                }

                if (test.TryGetValue(word, out CommandList result)) {
                    tokens.Add(new CommandToken(result));
                    continue;
                }

                tokens.Add(new ValueToken(new string(word)));
            }

            return tokens;
        }

        private static void SkipStartingSpace(ref int i, ReadOnlySpan<char> str)
        {
            Debug.Assert(str.Length > i);

            for (; i < str.Length; i++)
            {
                char c = str[i];

                if (c != '\r' && c != '\n' && !char.IsWhiteSpace(c))
                {
                    return;
                }
            }
        }

        private static ReadOnlySpan<char> ReadNextWord(ref int i, ReadOnlySpan<char> str)
        {
            char c;
            int start, end;

            for (c = str[i]; i < str.Length && char.IsWhiteSpace(c); i++, c = str[i])
                ;

            start = i;

            for (c = str[i]; i < str.Length && !char.IsWhiteSpace(c); i++, c = str[i])
                ;

            end = i;

            return str[start..end];
        }

        private static bool SkipCommentBlock(ref int i, ReadOnlySpan<char> str)
        {
            bool foundNewLine = false;

            for (; i < str.Length; i++)
            {
                ReadOnlySpan<char> subStr = str[i..];

                if (subStr.StartsWith("*/"))
                {
                    i += 2;
                    break;
                }

                foundNewLine |= str[i] == '\n';
            }

            return foundNewLine;
        }

        private static void SkipToNextLine(ref int i, ReadOnlySpan<char> str)
        {
            for (bool foundNewLineChar = false; i < str.Length; i++)
            {
                char c = str[i];

                if (c == '\r' || c == '\n')
                {
                    foundNewLineChar = true;
                    continue;
                }

                if (foundNewLineChar)
                {
                    break;
                }
            }
        }
    }
}
