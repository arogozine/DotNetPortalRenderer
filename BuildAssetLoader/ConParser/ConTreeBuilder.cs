namespace BuildAssetLoader.Con
{
    /// <summary>
    /// Builds a <see cref="Command"/> tree from a flat <see cref="ConToken"/> stream produced by <see cref="ConParser"/>.
    /// AI Assisted
    /// </summary>
    public static partial class ConTreeBuilder
    {
        public static List<Command> Build(IReadOnlyList<ConToken> tokens)
        {
            List<ConToken> filtered = new(tokens.Count);

            foreach (ConToken token in tokens)
            {
                if (token.ConTokenType != ConTokenType.NewLine)
                {
                    filtered.Add(token);
                }
            }

            ConTreeCursor cursor = new(filtered);
            return ParseStatements(cursor, insideBody: false, stop: null);
        }

        // A trailing `else` binds to the innermost still-open (ElseBody == null) conditional most recently
        // added to this statement list, even across decorative brace boundaries (see the comment on
        // FindPendingTail). `pending` tracks that target across loop iterations, including brace-skip ones.
        private static List<Command> ParseStatements(ConTreeCursor cursor, bool insideBody, Func<ConTreeCursor, bool>? stop)
        {
            List<Command> statements = [];
            ConditionalStructure? pending = null;

            while (!cursor.AtEnd && (stop is null || !stop(cursor)))
            {
                // Some GAME.CON bodies (e.g. state ... ends) are wrapped in a decorative, otherwise-unowned
                // { } pair with no associated if/while/case. Skip such stray brace tokens as no-ops; braces
                // that belong to a conditional/loop/case are already consumed by their own Expect calls.
                if (cursor.IsBlockStart)
                {
                    cursor.ExpectBlockStart();
                    continue;
                }

                if (cursor.IsBlockEnd)
                {
                    cursor.ExpectBlockEnd();
                    continue;
                }

                if (pending is not null && cursor.IsCommand(CommandList.else_))
                {
                    cursor.ExpectCommand(CommandList.else_);
                    List<Command> elseBranch = ParseBranch(cursor);
                    pending.ElseBody = elseBranch;
                    pending = FindPendingTail(elseBranch);
                    continue;
                }

                Command statement = ParseCommand(cursor, insideBody);
                statements.Add(statement);
                pending = statement as ConditionalStructure;
            }

            return statements;
        }

        /// <summary>Real GAME.CON attaches a trailing `else` to the innermost open conditional at the tail of
        /// the branch just parsed, even across brace boundaries, e.g.:
        ///   ifcount 48
        ///     resetcount
        ///   else
        ///   {
        ///     ifcount 32
        ///     sizeto 32 32
        ///   }
        ///   else
        ///     ifcount 16 { ... }
        /// attaches the second `else` to the nested `ifcount 32`, not to `ifcount 48` (which already has one).</summary>
        private static ConditionalStructure? FindPendingTail(List<Command> branch) =>
            branch is [.., ConditionalStructure { ElseBody: null } tail] ? tail : null;

        private static Command ParseCommand(ConTreeCursor cursor, bool insideBody)
        {
            if (!cursor.TryPeekCommand(out CommandList peeked))
            {
                return ParseStructureAccessFallback(cursor);
            }

            switch (peeked)
            {
                case CommandList.actor or CommandList.useractor:
                    return ParseActorStructure(cursor);

                case CommandList.onevent or CommandList.appendevent:
                    return ParseEventStructure(cursor);

                case CommandList.eventloadactor:
                    return ParseEventloadactorStructure(cursor);

                case CommandList.state or CommandList.defstate or CommandList.prependstate or CommandList.appendstate:
                    return ParseStateStructureOrInvoke(cursor, insideBody);

                case CommandList.switch_:
                    return ParseSwitch(cursor);

                case CommandList.whilevarl or CommandList.whilevare or CommandList.whilevarn
                    or CommandList.whilevarvarl or CommandList.whilevarvarn:
                    return ParseWhileLoop(cursor);

                case CommandList.move:
                    return insideBody ? ParseMoveInvoke(cursor) : ParseMoveDeclare(cursor);

                case CommandList.ai:
                    return insideBody ? ParseAiInvoke(cursor) : ParseAiDeclare(cursor);

                case CommandList.action:
                    return ParseAction(cursor);
            }

            if (IsConditional(peeked))
            {
                return ParseConditional(cursor);
            }

            CommandList command = cursor.ExpectCommand();
            return ParseFlatCommand(command, cursor);
        }

        private static bool IsConditional(CommandList command) =>
            command.ToString().StartsWith("if", StringComparison.Ordinal);

        // ===== Structures (actor/useractor, onevent/appendevent, state family) =====

        private static BaseActorCommand ParseActorStructure(ConTreeCursor cursor)
        {
            CommandList start = cursor.ExpectCommand();
            int n = cursor.CountContiguousValues();
            string[] args = cursor.ReadValues(n);

            List<Command> body = ParseStatements(cursor, insideBody: true, static (c) => c.IsCommand(CommandList.enda));
            cursor.ExpectCommand(CommandList.enda);

            BaseActorCommand actorCommand;

            if (start == CommandList.actor)
            {
                string picNum = args[0];
                string? strength = n > 1 ? args[1] : null;
                string? action = n > 2 ? args[2] : null;
                string? move = n > 3 ? args[3] : null;
                string[]? moveFlags = n > 4 ? args[4..] : null;
                actorCommand = new ActorCommand(picNum, strength, action, move, moveFlags);
            }
            else
            {
                if (n < 2)
                {
                    throw new FormatException("useractor requires a type and a tile number.");
                }

                string type = args[0];
                string picNum = args[1];
                string? strength = n > 2 ? args[2] : null;
                string? action = n > 3 ? args[3] : null;
                string? move = n > 4 ? args[4] : null;
                string[]? moveFlags = n > 5 ? args[5..] : null;
                actorCommand = new UserActorCommand(type, picNum, strength, action, move, moveFlags);
            }

            actorCommand.Body.AddRange(body);
            return actorCommand;
        }

        private static BaseEventCommand ParseEventStructure(ConTreeCursor cursor)
        {
            CommandList start = cursor.ExpectCommand();
            string eventName = cursor.ReadValue();

            List<Command> body = ParseStatements(cursor, insideBody: true, c => c.IsCommand(CommandList.endevent));
            cursor.ExpectCommand(CommandList.endevent);

            BaseEventCommand eventCommand = start == CommandList.onevent
                ? new OneventCommand(eventName)
                : new AppendeventCommand(eventName);

            eventCommand.Body.AddRange(body);
            return eventCommand;
        }

        private static Command ParseStateStructureOrInvoke(ConTreeCursor cursor, bool insideBody)
        {
            CommandList start = cursor.ExpectCommand();

            if (start == CommandList.state && insideBody)
            {
                return new StateInvokeCommand(cursor.ReadValue());
            }

            string name = cursor.ReadValue();
            List<Command> body = ParseStatements(cursor, insideBody: true, c => c.IsCommand(CommandList.ends));
            cursor.ExpectCommand(CommandList.ends);

            BaseStateCommand stateCommand = start switch
            {
                CommandList.state => new StateCommand(name),
                CommandList.defstate => new DefstateCommand(name),
                CommandList.prependstate => new PrependstateCommand(name),
                CommandList.appendstate => new AppendstateCommand(name),
                _ => throw new FormatException($"Unexpected state-family command '{start}'."),
            };

            stateCommand.Body.AddRange(body);
            return stateCommand;
        }

        // eventloadactor <name/tilenum> { ... } enda (Commands.Screen.cs; deprecated but still a Structure).
        private static EventloadactorCommand ParseEventloadactorStructure(ConTreeCursor cursor)
        {
            cursor.ExpectCommand(CommandList.eventloadactor);
            string actorName = cursor.ReadValue();

            List<Command> body = ParseStatements(cursor, insideBody: true, static (c) => c.IsCommand(CommandList.enda));
            cursor.ExpectCommand(CommandList.enda);

            EventloadactorCommand eventloadactor = new(actorName);
            eventloadactor.Body.AddRange(body);
            return eventloadactor;
        }

        // ===== Switch =====

        private static SwitchCommand ParseSwitch(ConTreeCursor cursor)
        {
            cursor.ExpectCommand(CommandList.switch_);
            string gamevar = cursor.ReadValue();
            List<CaseBlock> cases = [];

            while (cursor.IsCommand(CommandList.case_) || cursor.IsCommand(CommandList.default_))
            {
                bool isDefault = cursor.IsCommand(CommandList.default_);
                int? constant = null;

                if (isDefault)
                {
                    cursor.ExpectCommand(CommandList.default_);
                }
                else
                {
                    cursor.ExpectCommand(CommandList.case_);
                    constant = cursor.ReadInt();
                }

                cases.Add(new CaseBlock(constant, isDefault, ParseCaseBody(cursor)));
            }

            cursor.ExpectCommand(CommandList.endswitch);
            return new SwitchCommand(gamevar, cases);
        }

        private static List<Command> ParseCaseBody(ConTreeCursor cursor)
        {
            if (cursor.IsBlockStart)
            {
                cursor.ExpectBlockStart();
                List<Command> braced = ParseStatements(cursor, insideBody: true, static (c) => c.IsBlockEnd);
                cursor.ExpectBlockEnd();
                return braced;
            }

            return ParseStatements(cursor, insideBody: true,
                c => c.IsCommand(CommandList.case_) || c.IsCommand(CommandList.default_) || c.IsCommand(CommandList.endswitch));
        }

        // ===== Conditionals (if*) =====

        // Trailing-else attachment is handled uniformly by ParseStatements/ParseBranch (see FindPendingTail);
        // this only builds the leaf and true-branch.
        private static ConditionalStructure ParseConditional(ConTreeCursor cursor)
        {
            CommandList command = cursor.ExpectCommand();
            ConditionalStructure structure = ParseConditionalLeaf(command, cursor);
            structure.Body.AddRange(ParseBranch(cursor));
            return structure;
        }

        private static List<Command> ParseBranch(ConTreeCursor cursor)
        {
            if (cursor.IsBlockStart)
            {
                cursor.ExpectBlockStart();
                List<Command> body = ParseStatements(cursor, insideBody: true, static (c) => c.IsBlockEnd);
                cursor.ExpectBlockEnd();
                return body;
            }

            Command statement = ParseCommand(cursor, insideBody: true);
            ConditionalStructure? pending = statement as ConditionalStructure;

            while (pending is not null && cursor.IsCommand(CommandList.else_))
            {
                cursor.ExpectCommand(CommandList.else_);
                List<Command> elseBranch = ParseBranch(cursor);
                pending.ElseBody = elseBranch;
                pending = FindPendingTail(elseBranch);
            }

            return [statement];
        }

        // ===== Loops (whilevar*) =====

        private static LoopStructure ParseWhileLoop(ConTreeCursor cursor)
        {
            CommandList command = cursor.ExpectCommand();
            string gamevar = cursor.ReadValue();
            string operand = cursor.ReadValue();

            cursor.ExpectBlockStart();
            List<Command> body = ParseStatements(cursor, insideBody: true, static (c) => c.IsBlockEnd);
            cursor.ExpectBlockEnd();

            LoopStructure loop = command switch
            {
                CommandList.whilevarl or CommandList.whilevare or CommandList.whilevarn =>
                    new WhileVarCommand(command, WhileConditionLookup.Map[command], gamevar, operand),
                CommandList.whilevarvarl or CommandList.whilevarvarn =>
                    new WhileVarVarCommand(command, WhileConditionLookup.Map[command], gamevar, operand),
                _ => throw new FormatException($"Unexpected loop command '{command}'."),
            };

            loop.Body.AddRange(body);
            return loop;
        }

        // ===== move / ai / action (declaration vs. invocation is context-sensitive) =====

        private static MoveCommand ParseMoveDeclare(ConTreeCursor cursor)
        {
            cursor.ExpectCommand(CommandList.move);
            string name = cursor.ReadValue();
            int? horizontal = cursor.TryReadInt();
            int? vertical = cursor.TryReadInt();
            return new MoveCommand(name, horizontal, vertical);
        }

        private static MoveInvokeCommand ParseMoveInvoke(ConTreeCursor cursor)
        {
            cursor.ExpectCommand(CommandList.move);
            string name = cursor.ReadValue();
            int n = cursor.CountContiguousValues();
            string[]? flags = n > 0 ? cursor.ReadValues(n) : null;
            return new MoveInvokeCommand(name, flags);
        }

        private static AiCommand ParseAiDeclare(ConTreeCursor cursor)
        {
            cursor.ExpectCommand(CommandList.ai);
            int n = cursor.CountContiguousValues();
            string[] args = cursor.ReadValues(n);
            string name = args[0];
            string? action = n > 1 ? args[1] : null;
            string? move = n > 2 ? args[2] : null;
            string[]? moveFlags = n > 3 ? args[3..] : null;
            return new AiCommand(name, action, move, moveFlags);
        }

        private static AiInvokeCommand ParseAiInvoke(ConTreeCursor cursor)
        {
            cursor.ExpectCommand(CommandList.ai);
            return new AiInvokeCommand(cursor.ReadValue());
        }

        private static ActionCommand ParseAction(ConTreeCursor cursor)
        {
            cursor.ExpectCommand(CommandList.action);
            int n = cursor.CountContiguousValues();
            string[] args = cursor.ReadValues(n);
            string name = args[0];
            int? startFrame = n > 1 ? int.Parse(args[1]) : null;
            int? frames = n > 2 ? int.Parse(args[2]) : null;
            int? viewType = n > 3 ? int.Parse(args[3]) : null;
            int? incValue = n > 4 ? int.Parse(args[4]) : null;
            int? delay = n > 5 ? int.Parse(args[5]) : null;
            return new ActionCommand(name, startFrame, frames, viewType, incValue, delay);
        }

        // ===== Flat command dispatch =====

        private static Command ParseFlatCommand(CommandList command, ConTreeCursor cursor) =>
            TryParseActor(command, cursor)
            ?? TryParseAudio(command, cursor)
            ?? TryParseFlow(command, cursor)
            ?? TryParseGamevar(command, cursor)
            ?? TryParseMetaSettings(command, cursor)
            ?? TryParsePlayer(command, cursor)
            ?? TryParsePreprocessor(command, cursor)
            ?? TryParseScreen(command, cursor)
            ?? TryParseSectors(command, cursor)
            ?? TryParseSpawning(command, cursor)
            ?? TryParseStructureAccess(command, cursor)
            ?? throw new FormatException($"No flat-command mapping for '{command}' at index {cursor.Position}.");

        // ===== getactor[id].member-style structure access (glued token; see Commands.StructureAccess.cs) =====

        private static Command ParseStructureAccessFallback(ConTreeCursor cursor)
        {
            string raw = cursor.ReadValue();
            int bracketOpen = raw.IndexOf('[');
            int bracketClose = bracketOpen >= 0 ? raw.IndexOf(']', bracketOpen + 1) : -1;
            int dot = bracketClose >= 0 ? raw.IndexOf('.', bracketClose + 1) : -1;

            if (bracketOpen < 0 || bracketClose < 0 || dot < 0)
            {
                throw new FormatException($"Unexpected value token '{raw}' where a command was expected.");
            }

            string prefix = raw[..bracketOpen];
            string id = raw[(bracketOpen + 1)..bracketClose];
            string member = raw[(dot + 1)..];
            string? idOrNull = id.Length == 0 ? null : id;
            string operand = cursor.ReadValue();

            return prefix switch
            {
                "getactor" => new GetActorCommand(idOrNull, member, operand),
                "setactor" => new SetActorCommand(idOrNull, member, operand),
                "getactorvar" => new GetActorVarCommand(idOrNull, member, operand),
                "setactorvar" => new SetActorVarCommand(idOrNull, member, operand),
                "getinput" => new GetInputCommand(idOrNull, member, operand),
                "setinput" => new SetInputCommand(idOrNull, member, operand),
                "getplayer" => new GetPlayerCommand(idOrNull, member, operand),
                "setplayer" => new SetPlayerCommand(idOrNull, member, operand),
                "getplayervar" => new GetPlayerVarCommand(idOrNull, member, operand),
                "setplayervar" => new SetPlayerVarCommand(idOrNull, member, operand),
                "getprojectile" => new GetProjectileCommand(idOrNull, member, operand),
                "setprojectile" => new SetProjectileCommand(idOrNull, member, operand),
                "getsector" => new GetSectorCommand(idOrNull, member, operand),
                "setsector" => new SetSectorCommand(idOrNull, member, operand),
                "getthisprojectile" => new GetThisProjectileCommand(idOrNull, member, operand),
                "setthisprojectile" => new SetThisProjectileCommand(idOrNull, member, operand),
                "gettspr" => new GetTsprCommand(idOrNull, member, operand),
                "settspr" => new SetTsprCommand(idOrNull, member, operand),
                "getuserdef" => new GetUserdefCommand(member, operand),
                "setuserdef" => new SetUserdefCommand(member, operand),
                "getwall" => new GetWallCommand(idOrNull, member, operand),
                "setwall" => new SetWallCommand(idOrNull, member, operand),
                _ => throw new FormatException($"Unrecognized structure-access token '{raw}'."),
            };
        }

        /// <summary>Splits a glued "name[index]" token (e.g. setarray/copy operands) into its parts.</summary>
        private static (string Name, string Index) SplitIndexed(string raw)
        {
            int open = raw.IndexOf('[');

            if (open < 0)
            {
                return (raw, "");
            }

            int close = raw.IndexOf(']', open + 1);
            string name = raw[..open];
            string index = close > open ? raw[(open + 1)..close] : "";
            return (name, index);
        }
    }
}
