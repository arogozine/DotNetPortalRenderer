// AI Assisted
using System.Text;

namespace DoomAssetLoader.Decorate
{
    /// <summary>
    /// Parses the text-based DECORATE actor-definition lump used by ZDoom-family source ports.
    /// Property statements are treated as line-terminated (real-world DECORATE is conventionally
    /// written one statement per line, and the format has no statement terminator otherwise - the
    /// reference implementation resolves this via an internal per-property argument-count table,
    /// which is out of scope here). Anonymous multi-statement action blocks are captured as raw,
    /// unparsed source text rather than evaluated.
    /// </summary>
    public static class DecorateParser
    {
        public static List<DecorateActor> Parse(ReadOnlySpan<char> decorateContent)
        {
            List<DecorateToken> tokens = DecorateLexer.Tokenize(decorateContent);
            List<DecorateActor> actors = [];
            int pos = 0;

            while (true)
            {
                SkipEndOfLines(tokens, ref pos);

                DecorateToken token = Current(tokens, pos);

                if (token.Type == DecorateTokenType.EndOfFile)
                {
                    break;
                }

                if (token.Type == DecorateTokenType.Word && token.Text.Equals("#include", StringComparison.OrdinalIgnoreCase))
                {
                    Advance(tokens, ref pos);

                    if (Current(tokens, pos).Type == DecorateTokenType.String)
                    {
                        Advance(tokens, ref pos);
                    }

                    continue;
                }

                if (token.Type == DecorateTokenType.Word && token.Text.Equals("actor", StringComparison.OrdinalIgnoreCase))
                {
                    actors.Add(ParseActor(tokens, ref pos));
                    continue;
                }

                Advance(tokens, ref pos);
            }

            return actors;
        }

        private static DecorateActor ParseActor(List<DecorateToken> tokens, ref int pos)
        {
            Advance(tokens, ref pos);
            SkipEndOfLines(tokens, ref pos);

            string name = ExpectWord(tokens, ref pos);

            string? parent = null;
            string? replaces = null;
            int? doomEdNum = null;

            SkipEndOfLines(tokens, ref pos);

            if (Current(tokens, pos) is { Type: DecorateTokenType.Symbol, Text: ":" })
            {
                Advance(tokens, ref pos);
                SkipEndOfLines(tokens, ref pos);
                parent = ExpectWord(tokens, ref pos);
                SkipEndOfLines(tokens, ref pos);
            }

            if (Current(tokens, pos) is { Type: DecorateTokenType.Word } replacesToken &&
                replacesToken.Text.Equals("replaces", StringComparison.OrdinalIgnoreCase))
            {
                Advance(tokens, ref pos);
                SkipEndOfLines(tokens, ref pos);
                replaces = ExpectWord(tokens, ref pos);
                SkipEndOfLines(tokens, ref pos);
            }

            if (Current(tokens, pos) is { Type: DecorateTokenType.Word } numberToken &&
                int.TryParse(numberToken.Text, out int parsedDoomEdNum))
            {
                doomEdNum = parsedDoomEdNum;
                Advance(tokens, ref pos);
                SkipEndOfLines(tokens, ref pos);
            }

            _ = Expect(tokens, ref pos, DecorateTokenType.Symbol, "{");

            var actor = new DecorateActor
            {
                Name = name,
                Parent = parent,
                Replaces = replaces,
                DoomEdNum = doomEdNum
            };

            ParseActorBody(tokens, ref pos, actor);

            _ = Expect(tokens, ref pos, DecorateTokenType.Symbol, "}");

            return actor;
        }

        private static void ParseActorBody(List<DecorateToken> tokens, ref int pos, DecorateActor actor)
        {
            while (true)
            {
                SkipEndOfLines(tokens, ref pos);

                DecorateToken token = Current(tokens, pos);

                if (token.Type == DecorateTokenType.Symbol && token.Text == "}")
                {
                    return;
                }

                if (token.Type == DecorateTokenType.EndOfFile)
                {
                    throw new FormatException("Unexpected end of DECORATE content inside an actor body.");
                }

                if (token.Type == DecorateTokenType.Word && token.Text.Equals("states", StringComparison.OrdinalIgnoreCase))
                {
                    ParseStates(tokens, ref pos, actor);
                    continue;
                }

                if (token.Type == DecorateTokenType.Symbol && (token.Text == "+" || token.Text == "-"))
                {
                    ParseFlag(tokens, ref pos, actor);
                    continue;
                }

                if (token.Type == DecorateTokenType.Word)
                {
                    ParseProperty(tokens, ref pos, actor);
                    continue;
                }

                Advance(tokens, ref pos);
            }
        }

        private static void ParseFlag(List<DecorateToken> tokens, ref int pos, DecorateActor actor)
        {
            bool value = Current(tokens, pos).Text == "+";
            Advance(tokens, ref pos);
            string name = ExpectWord(tokens, ref pos);
            actor.Flags[name] = value;
        }

        private static void ParseProperty(List<DecorateToken> tokens, ref int pos, DecorateActor actor)
        {
            string name = ExpectWord(tokens, ref pos);
            List<string> arguments = ReadArgumentsUntilLineEnd(tokens, ref pos);
            actor.Properties[name] = new DecorateProperty(name, arguments);
        }

        private static List<string> ReadArgumentsUntilLineEnd(List<DecorateToken> tokens, ref int pos)
        {
            List<string> arguments = [];
            StringBuilder current = new();
            int parenDepth = 0;
            bool hasContent = false;

            while (true)
            {
                DecorateToken token = Current(tokens, pos);

                if (parenDepth == 0 &&
                    (token.Type == DecorateTokenType.EndOfLine ||
                     token.Type == DecorateTokenType.EndOfFile ||
                     (token.Type == DecorateTokenType.Symbol && (token.Text == ";" || token.Text == "}"))))
                {
                    break;
                }

                if (parenDepth == 0 && token.Type == DecorateTokenType.Symbol && token.Text == ",")
                {
                    arguments.Add(current.ToString());
                    _ = current.Clear();
                    hasContent = false;
                    Advance(tokens, ref pos);
                    continue;
                }

                if (token.Type == DecorateTokenType.Symbol && token.Text == "(")
                {
                    parenDepth++;
                }
                else if (token.Type == DecorateTokenType.Symbol && token.Text == ")")
                {
                    parenDepth--;
                }

                _ = current.Append(token.Text);
                hasContent = true;
                Advance(tokens, ref pos);
            }

            if (hasContent || arguments.Count > 0)
            {
                arguments.Add(current.ToString());
            }

            return arguments;
        }

        private static List<string> ReadParenthesizedArguments(List<DecorateToken> tokens, ref int pos)
        {
            _ = Expect(tokens, ref pos, DecorateTokenType.Symbol, "(");

            List<string> arguments = [];
            StringBuilder current = new();
            int depth = 0;
            bool hasContent = false;

            while (true)
            {
                DecorateToken token = Current(tokens, pos);

                if (token.Type == DecorateTokenType.EndOfFile)
                {
                    throw new FormatException("Unexpected end of DECORATE content inside a parenthesized argument list.");
                }

                if (depth == 0 && token.Type == DecorateTokenType.Symbol && token.Text == ")")
                {
                    Advance(tokens, ref pos);
                    break;
                }

                if (depth == 0 && token.Type == DecorateTokenType.Symbol && token.Text == ",")
                {
                    arguments.Add(current.ToString());
                    _ = current.Clear();
                    hasContent = false;
                    Advance(tokens, ref pos);
                    continue;
                }

                if (token.Type == DecorateTokenType.EndOfLine)
                {
                    Advance(tokens, ref pos);
                    continue;
                }

                if (token.Type == DecorateTokenType.Symbol && token.Text == "(")
                {
                    depth++;
                }
                else if (token.Type == DecorateTokenType.Symbol && token.Text == ")")
                {
                    depth--;
                }

                _ = current.Append(token.Text);
                hasContent = true;
                Advance(tokens, ref pos);
            }

            if (hasContent || arguments.Count > 0)
            {
                arguments.Add(current.ToString());
            }

            return arguments;
        }

        private static string ReadRawBlock(List<DecorateToken> tokens, ref int pos)
        {
            Expect(tokens, ref pos, DecorateTokenType.Symbol, "{");

            StringBuilder builder = new();
            int depth = 1;

            while (depth > 0)
            {
                DecorateToken token = Current(tokens, pos);

                if (token.Type == DecorateTokenType.EndOfFile)
                {
                    throw new FormatException("Unexpected end of DECORATE content inside an anonymous action block.");
                }

                if (token.Type == DecorateTokenType.EndOfLine)
                {
                    Advance(tokens, ref pos);
                    continue;
                }

                if (token.Type == DecorateTokenType.Symbol && token.Text == "{")
                {
                    depth++;
                }
                else if (token.Type == DecorateTokenType.Symbol && token.Text == "}")
                {
                    depth--;

                    if (depth == 0)
                    {
                        Advance(tokens, ref pos);
                        break;
                    }
                }

                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                _ = builder.Append(token.Text);
                Advance(tokens, ref pos);
            }

            return builder.ToString();
        }

        private static string ReadDuration(List<DecorateToken> tokens, ref int pos)
        {
            DecorateToken token = Current(tokens, pos);

            if (token.Type == DecorateTokenType.Word && token.Text.Equals("random", StringComparison.OrdinalIgnoreCase))
            {
                Advance(tokens, ref pos);
                List<string> args = ReadParenthesizedArguments(tokens, ref pos);
                return $"random({string.Join(",", args)})";
            }

            if (token.Type == DecorateTokenType.Symbol && token.Text == "(")
            {
                List<string> args = ReadParenthesizedArguments(tokens, ref pos);
                return "(" + string.Join(",", args) + ")";
            }

            string sign = "";

            if (token.Type == DecorateTokenType.Symbol && token.Text == "-")
            {
                sign = "-";
                Advance(tokens, ref pos);
            }

            return sign + ExpectWord(tokens, ref pos);
        }

        private static void ParseStates(List<DecorateToken> tokens, ref int pos, DecorateActor actor)
        {
            Advance(tokens, ref pos);
            SkipEndOfLines(tokens, ref pos);

            if (Current(tokens, pos) is { Type: DecorateTokenType.Symbol, Text: "(" })
            {
                ReadParenthesizedArguments(tokens, ref pos);
                SkipEndOfLines(tokens, ref pos);
            }

            Expect(tokens, ref pos, DecorateTokenType.Symbol, "{");

            while (true)
            {
                SkipEndOfLines(tokens, ref pos);

                DecorateToken token = Current(tokens, pos);

                if (token.Type == DecorateTokenType.Symbol && token.Text == "}")
                {
                    Advance(tokens, ref pos);
                    return;
                }

                if (token.Type == DecorateTokenType.EndOfFile)
                {
                    throw new FormatException("Unexpected end of DECORATE content inside a states block.");
                }

                if (token.Type == DecorateTokenType.Word && IsFlowKeyword(token.Text))
                {
                    actor.States.Add(ParseFlowControl(tokens, ref pos));
                    continue;
                }

                if (token.Type == DecorateTokenType.Word && PeekIsLabel(tokens, pos))
                {
                    Advance(tokens, ref pos);
                    Advance(tokens, ref pos);
                    actor.States.Add(new DecorateStateLabel(token.Text));
                    continue;
                }

                if (token.Type == DecorateTokenType.Word)
                {
                    actor.States.Add(ParseStateDefinition(tokens, ref pos));
                    continue;
                }

                Advance(tokens, ref pos);
            }
        }

        private static bool PeekIsLabel(List<DecorateToken> tokens, int pos) =>
            pos + 1 < tokens.Count && tokens[pos + 1] is { Type: DecorateTokenType.Symbol, Text: ":" };

        private static bool IsFlowKeyword(string text) =>
            text.Equals("loop", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("stop", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("wait", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("fail", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("goto", StringComparison.OrdinalIgnoreCase);

        private static DecorateFlowControl ParseFlowControl(List<DecorateToken> tokens, ref int pos)
        {
            string keyword = Current(tokens, pos).Text;
            Advance(tokens, ref pos);

            if (keyword.Equals("loop", StringComparison.OrdinalIgnoreCase))
            {
                return new DecorateFlowControl(DecorateFlowControlKind.Loop);
            }

            if (keyword.Equals("stop", StringComparison.OrdinalIgnoreCase))
            {
                return new DecorateFlowControl(DecorateFlowControlKind.Stop);
            }

            if (keyword.Equals("wait", StringComparison.OrdinalIgnoreCase))
            {
                return new DecorateFlowControl(DecorateFlowControlKind.Wait);
            }

            if (keyword.Equals("fail", StringComparison.OrdinalIgnoreCase))
            {
                return new DecorateFlowControl(DecorateFlowControlKind.Fail);
            }

            string label = ExpectWord(tokens, ref pos);
            string? scope = null;

            if (Current(tokens, pos) is { Type: DecorateTokenType.Symbol, Text: "::" })
            {
                Advance(tokens, ref pos);
                scope = label;
                label = ExpectWord(tokens, ref pos);
            }

            int offset = 0;

            if (Current(tokens, pos) is { Type: DecorateTokenType.Symbol, Text: "+" })
            {
                Advance(tokens, ref pos);
                offset = int.Parse(ExpectWord(tokens, ref pos));
            }

            return new DecorateFlowControl(DecorateFlowControlKind.Goto, label, scope, offset);
        }

        private static DecorateStateDefinition ParseStateDefinition(List<DecorateToken> tokens, ref int pos)
        {
            string sprite = ExpectWord(tokens, ref pos);

            DecorateToken framesToken = Current(tokens, pos);
            string frames = framesToken.Type == DecorateTokenType.String
                ? framesToken.Text
                : ExpectWord(tokens, ref pos);

            if (framesToken.Type == DecorateTokenType.String)
            {
                Advance(tokens, ref pos);
            }

            string duration = ReadDuration(tokens, ref pos);

            bool bright = false;
            bool canRaise = false;
            bool fast = false;
            bool slow = false;
            bool noDelay = false;
            string? light = null;
            (int X, int Y)? offset = null;

            while (Current(tokens, pos).Type == DecorateTokenType.Word)
            {
                string keyword = Current(tokens, pos).Text;

                if (keyword.Equals("bright", StringComparison.OrdinalIgnoreCase))
                {
                    bright = true;
                    Advance(tokens, ref pos);
                    continue;
                }

                if (keyword.Equals("canraise", StringComparison.OrdinalIgnoreCase))
                {
                    canRaise = true;
                    Advance(tokens, ref pos);
                    continue;
                }

                if (keyword.Equals("fast", StringComparison.OrdinalIgnoreCase))
                {
                    fast = true;
                    Advance(tokens, ref pos);
                    continue;
                }

                if (keyword.Equals("slow", StringComparison.OrdinalIgnoreCase))
                {
                    slow = true;
                    Advance(tokens, ref pos);
                    continue;
                }

                if (keyword.Equals("nodelay", StringComparison.OrdinalIgnoreCase))
                {
                    noDelay = true;
                    Advance(tokens, ref pos);
                    continue;
                }

                if (keyword.Equals("light", StringComparison.OrdinalIgnoreCase))
                {
                    Advance(tokens, ref pos);
                    List<string> args = ReadParenthesizedArguments(tokens, ref pos);
                    light = args.Count > 0 ? args[0] : null;
                    continue;
                }

                if (keyword.Equals("offset", StringComparison.OrdinalIgnoreCase))
                {
                    Advance(tokens, ref pos);
                    List<string> args = ReadParenthesizedArguments(tokens, ref pos);
                    offset = (
                        args.Count > 0 ? int.Parse(args[0]) : 0,
                        args.Count > 1 ? int.Parse(args[1]) : 0
                    );
                    continue;
                }

                break;
            }

            string? actionFunction = null;
            List<string>? actionArguments = null;
            string? rawActionBlock = null;

            DecorateToken next = Current(tokens, pos);

            // An anonymous action block's opening brace conventionally sits on its own line, so it is
            // looked for past any end-of-line tokens. A bare action-function word is not: whether one
            // is present has no grammar-level terminator, so (mirroring the line-terminated property
            // parsing above) only a word still on the same line as the state definition counts.
            int blockLookaheadPos = pos;

            while (Current(tokens, blockLookaheadPos).Type == DecorateTokenType.EndOfLine)
            {
                Advance(tokens, ref blockLookaheadPos);
            }

            if (Current(tokens, blockLookaheadPos) is { Type: DecorateTokenType.Symbol, Text: "{" })
            {
                pos = blockLookaheadPos;
                rawActionBlock = ReadRawBlock(tokens, ref pos);
            }
            else if (next.Type == DecorateTokenType.Word && !IsFlowKeyword(next.Text))
            {
                actionFunction = next.Text;
                Advance(tokens, ref pos);

                if (Current(tokens, pos) is { Type: DecorateTokenType.Symbol, Text: "(" })
                {
                    actionArguments = ReadParenthesizedArguments(tokens, ref pos);
                }
            }

            return new DecorateStateDefinition(
                sprite, frames, duration, bright, canRaise, fast, slow, noDelay,
                light, offset, actionFunction, actionArguments, rawActionBlock);
        }

        private static DecorateToken Current(List<DecorateToken> tokens, int pos) => tokens[pos];

        private static void Advance(List<DecorateToken> tokens, ref int pos)
        {
            if (pos < tokens.Count - 1)
            {
                pos++;
            }
        }

        private static void SkipEndOfLines(List<DecorateToken> tokens, ref int pos)
        {
            while (Current(tokens, pos).Type == DecorateTokenType.EndOfLine)
            {
                Advance(tokens, ref pos);
            }
        }

        private static DecorateToken Expect(List<DecorateToken> tokens, ref int pos, DecorateTokenType type, string text)
        {
            DecorateToken token = Current(tokens, pos);

            if (token.Type != type || !token.Text.Equals(text, StringComparison.Ordinal))
            {
                throw new FormatException($"Expected '{text}' but found '{token.Text}' ({token.Type}).");
            }

            Advance(tokens, ref pos);
            return token;
        }

        private static string ExpectWord(List<DecorateToken> tokens, ref int pos)
        {
            DecorateToken token = Current(tokens, pos);

            if (token.Type != DecorateTokenType.Word)
            {
                throw new FormatException($"Expected a word but found '{token.Text}' ({token.Type}).");
            }

            Advance(tokens, ref pos);
            return token.Text;
        }
    }
}
