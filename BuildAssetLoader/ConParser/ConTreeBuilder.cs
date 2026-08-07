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

                if (pending is not null && cursor.IsCommand(CommandList.Else))
                {
                    cursor.ExpectCommand(CommandList.Else);
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
                case CommandList.Actor or CommandList.UserActor:
                    return ParseActorStructure(cursor);

                case CommandList.OnEvent or CommandList.AppendEvent:
                    return ParseEventStructure(cursor);

                case CommandList.EventLoadActor:
                    return ParseEventloadactorStructure(cursor);

                case CommandList.State or CommandList.DefState or CommandList.PrependState or CommandList.AppendState:
                    return ParseStateStructureOrInvoke(cursor, insideBody);

                case CommandList.Switch:
                    return ParseSwitch(cursor);

                case CommandList.WhileVarL or CommandList.WhileVarE or CommandList.WhileVarN
                    or CommandList.WhileVarVarL or CommandList.WhileVarVarN:
                    return ParseWhileLoop(cursor);

                case CommandList.Move:
                    return insideBody ? ParseMoveInvoke(cursor) : ParseMoveDeclare(cursor);

                case CommandList.Ai:
                    return insideBody ? ParseAiInvoke(cursor) : ParseAiDeclare(cursor);

                case CommandList.Action:
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
            command.ToString().StartsWith("If", StringComparison.Ordinal);

        // ===== Structures (actor/useractor, onevent/appendevent, state family) =====

        private static BaseActorCommand ParseActorStructure(ConTreeCursor cursor)
        {
            CommandList start = cursor.ExpectCommand();
            int n = cursor.CountContiguousValues();
            string[] args = cursor.ReadValues(n);

            List<Command> body = ParseStatements(cursor, insideBody: true, static (c) => c.IsCommand(CommandList.Enda));
            cursor.ExpectCommand(CommandList.Enda);

            BaseActorCommand actorCommand;

            if (start == CommandList.Actor)
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

            List<Command> body = ParseStatements(cursor, insideBody: true, c => c.IsCommand(CommandList.EndEvent));
            cursor.ExpectCommand(CommandList.EndEvent);

            BaseEventCommand eventCommand = start == CommandList.OnEvent
                ? new OnEventCommand(eventName)
                : new AppendEventCommand(eventName);

            eventCommand.Body.AddRange(body);
            return eventCommand;
        }

        private static Command ParseStateStructureOrInvoke(ConTreeCursor cursor, bool insideBody)
        {
            CommandList start = cursor.ExpectCommand();

            if (start == CommandList.State && insideBody)
            {
                return new StateInvokeCommand(cursor.ReadValue());
            }

            string name = cursor.ReadValue();
            List<Command> body = ParseStatements(cursor, insideBody: true, c => c.IsCommand(CommandList.Ends));
            cursor.ExpectCommand(CommandList.Ends);

            BaseStateCommand stateCommand = start switch
            {
                CommandList.State => new StateCommand(name),
                CommandList.DefState => new DefStateCommand(name),
                CommandList.PrependState => new PrependStateCommand(name),
                CommandList.AppendState => new AppendStateCommand(name),
                _ => throw new FormatException($"Unexpected state-family command '{start}'."),
            };

            stateCommand.Body.AddRange(body);
            return stateCommand;
        }

        // eventloadactor <name/tilenum> { ... } enda (Commands.Screen.cs; deprecated but still a Structure).
        private static EventLoadActorCommand ParseEventloadactorStructure(ConTreeCursor cursor)
        {
            cursor.ExpectCommand(CommandList.EventLoadActor);
            string actorName = cursor.ReadValue();

            List<Command> body = ParseStatements(cursor, insideBody: true, static (c) => c.IsCommand(CommandList.Enda));
            cursor.ExpectCommand(CommandList.Enda);

            EventLoadActorCommand eventloadactor = new(actorName);
            eventloadactor.Body.AddRange(body);
            return eventloadactor;
        }

        // ===== Switch =====

        private static SwitchCommand ParseSwitch(ConTreeCursor cursor)
        {
            cursor.ExpectCommand(CommandList.Switch);
            string gamevar = cursor.ReadValue();
            List<CaseBlock> cases = [];

            while (cursor.IsCommand(CommandList.Case) || cursor.IsCommand(CommandList.Default))
            {
                bool isDefault = cursor.IsCommand(CommandList.Default);
                int? constant = null;

                if (isDefault)
                {
                    cursor.ExpectCommand(CommandList.Default);
                }
                else
                {
                    cursor.ExpectCommand(CommandList.Case);
                    constant = cursor.ReadInt();
                }

                cases.Add(new CaseBlock(constant, isDefault, ParseCaseBody(cursor)));
            }

            cursor.ExpectCommand(CommandList.EndSwitch);
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
                c => c.IsCommand(CommandList.Case) || c.IsCommand(CommandList.Default) || c.IsCommand(CommandList.EndSwitch));
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

            while (pending is not null && cursor.IsCommand(CommandList.Else))
            {
                cursor.ExpectCommand(CommandList.Else);
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
                CommandList.WhileVarL or CommandList.WhileVarE or CommandList.WhileVarN =>
                    new WhileVarCommand(command, WhileConditionLookup.Map[command], gamevar, operand),
                CommandList.WhileVarVarL or CommandList.WhileVarVarN =>
                    new WhileVarVarCommand(command, WhileConditionLookup.Map[command], gamevar, operand),
                _ => throw new FormatException($"Unexpected loop command '{command}'."),
            };

            loop.Body.AddRange(body);
            return loop;
        }

        // ===== move / ai / action (declaration vs. invocation is context-sensitive) =====

        private static MoveCommand ParseMoveDeclare(ConTreeCursor cursor)
        {
            cursor.ExpectCommand(CommandList.Move);
            string name = cursor.ReadValue();
            int? horizontal = cursor.TryReadInt();
            int? vertical = cursor.TryReadInt();
            return new MoveCommand(name, horizontal, vertical);
        }

        private static MoveInvokeCommand ParseMoveInvoke(ConTreeCursor cursor)
        {
            cursor.ExpectCommand(CommandList.Move);
            string name = cursor.ReadValue();
            int n = cursor.CountContiguousValues();
            string[]? flags = n > 0 ? cursor.ReadValues(n) : null;
            return new MoveInvokeCommand(name, flags);
        }

        private static AiCommand ParseAiDeclare(ConTreeCursor cursor)
        {
            cursor.ExpectCommand(CommandList.Ai);
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
            cursor.ExpectCommand(CommandList.Ai);
            return new AiInvokeCommand(cursor.ReadValue());
        }

        private static ActionCommand ParseAction(ConTreeCursor cursor)
        {
            cursor.ExpectCommand(CommandList.Action);
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
                "getuserdef" => new GetUserDefCommand(member, operand),
                "setuserdef" => new SetUserDefCommand(member, operand),
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
