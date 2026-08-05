using System.Collections.Frozen;

namespace BuildAssetLoader.Con
{
    // ===== Base shapes =====

    public abstract record Command(CommandList StartStart);

    /// <summary>Block-scoped command that closes on an explicit terminating keyword (enda, ends, endevent, ...).</summary>
    public abstract record Structure(CommandList Start, CommandList End) : Command(Start)
    {
        public List<Command> Body { get; } = [];
    }

    /// <summary>if&lt;cond&gt; &lt;true-branch&gt; [else &lt;false-branch&gt;]; branch is either one statement or a {}-block. No explicit end keyword.</summary>
    public abstract record ConditionalStructure(CommandList Start) : Command(Start)
    {
        public List<Command> Body { get; } = [];
        public List<Command>? ElseBody { get; set; }
    }

    /// <summary>while*&lt;cond&gt; { ...body... }. No explicit end keyword.</summary>
    public abstract record LoopStructure(CommandList Start) : Command(Start)
    {
        public List<Command> Body { get; } = [];
    }

    /// <summary>One case (or default) block inside a switch. Not a standalone Command —
    /// CommandList.case_/CommandList.default_ are structural markers, consumed here rather than modeled as their own Command types (see §5).</summary>
    public sealed record CaseBlock(int? Constant, bool IsDefault, List<Command> Body);

    /// <summary>switch &lt;gamevar&gt; case &lt;c&gt; {...} break ... default {...} break endswitch.</summary>
    public sealed record SwitchCommand(string Gamevar, List<CaseBlock> Cases)
        : Structure(CommandList.switch_, CommandList.endswitch);

    // ===== Gamevar operator / condition enums =====

    public enum GamevarOperator { Set, Add, Sub, Mul, Div, Mod, And, Or, Xor, Rand }

    public enum GamevarCondition { Equal, NotEqual, Greater, Less, And, Or, Xor, Either }

    public static class GamevarOperatorLookup
    {
        public static readonly FrozenDictionary<CommandList, GamevarOperator> Map = new Dictionary<CommandList, GamevarOperator>
        {
            [CommandList.setvar] = GamevarOperator.Set,
            [CommandList.setvarvar] = GamevarOperator.Set,
            [CommandList.addvar] = GamevarOperator.Add,
            [CommandList.addvarvar] = GamevarOperator.Add,
            [CommandList.subvar] = GamevarOperator.Sub,
            [CommandList.subvarvar] = GamevarOperator.Sub,
            [CommandList.mulvar] = GamevarOperator.Mul,
            [CommandList.mulvarvar] = GamevarOperator.Mul,
            [CommandList.divvar] = GamevarOperator.Div,
            [CommandList.divvarvar] = GamevarOperator.Div,
            [CommandList.modvar] = GamevarOperator.Mod,
            [CommandList.modvarvar] = GamevarOperator.Mod,
            [CommandList.andvar] = GamevarOperator.And,
            [CommandList.andvarvar] = GamevarOperator.And,
            [CommandList.orvar] = GamevarOperator.Or,
            [CommandList.orvarvar] = GamevarOperator.Or,
            [CommandList.xorvar] = GamevarOperator.Xor,
            [CommandList.xorvarvar] = GamevarOperator.Xor,
            [CommandList.randvar] = GamevarOperator.Rand,
            [CommandList.randvarvar] = GamevarOperator.Rand,
        }.ToFrozenDictionary();
    }

    public static class GamevarConditionLookup
    {
        public static readonly FrozenDictionary<CommandList, GamevarCondition> Map = new Dictionary<CommandList, GamevarCondition>
        {
            [CommandList.ifvare] = GamevarCondition.Equal,
            [CommandList.ifvarvare] = GamevarCondition.Equal,
            [CommandList.ifvarn] = GamevarCondition.NotEqual,
            [CommandList.ifvarvarn] = GamevarCondition.NotEqual,
            [CommandList.ifvarg] = GamevarCondition.Greater,
            [CommandList.ifvarvarg] = GamevarCondition.Greater,
            [CommandList.ifvarl] = GamevarCondition.Less,
            [CommandList.ifvarvarl] = GamevarCondition.Less,
            [CommandList.ifvarand] = GamevarCondition.And,
            [CommandList.ifvarvarand] = GamevarCondition.And,
            [CommandList.ifvaror] = GamevarCondition.Or,
            [CommandList.ifvarvaror] = GamevarCondition.Or,
            [CommandList.ifvarxor] = GamevarCondition.Xor,
            [CommandList.ifvarvarxor] = GamevarCondition.Xor,
            [CommandList.ifvareither] = GamevarCondition.Either,
            [CommandList.ifvarvareither] = GamevarCondition.Either,
        }.ToFrozenDictionary();
    }

    // ===== Gamevar operator / condition records (Phase 4/3, declared here alongside the enums per §3.5) =====

    /// <summary>&lt;op&gt; &lt;gamevar&gt; &lt;constant|gamevar&gt; — second operand is a constant or a define/label (setvar, addvar, ...).</summary>
    public sealed record VarOpCommand(CommandList Start, GamevarOperator Operator, string Gamevar, string Value)
        : Command(Start);

    /// <summary>&lt;op&gt;varvar &lt;gamevar&gt; &lt;gamevar&gt; — second operand is itself a gamevar (setvarvar, addvarvar, ...).</summary>
    public sealed record VarVarOpCommand(CommandList Start, GamevarOperator Operator, string Gamevar, string OtherGamevar)
        : Command(Start);

    /// <summary>ifvar&lt;cond&gt; &lt;gamevar&gt; &lt;constant&gt; — second operand is a constant or define/label.</summary>
    public sealed record IfVarCommand(CommandList Start, GamevarCondition Condition, string Gamevar, string Value)
        : ConditionalStructure(Start);

    /// <summary>ifvarvar&lt;cond&gt; &lt;gamevar&gt; &lt;gamevar&gt; — both operands are gamevars.</summary>
    public sealed record IfVarVarCommand(CommandList Start, GamevarCondition Condition, string Gamevar, string OtherGamevar)
        : ConditionalStructure(Start);

    // ===== Preprocessor =====

    public record DefineCommand(string Name, int Number) : Command(CommandList.define);

    // ===== Global Settings - Object-Oriented =====

    public record ActionCommand(string Name, int? Startframe, int? Frames, int? ViewType, int? IncValue, int? Delay)
         : Command(CommandList.action);

    public record BaseActorCommand(CommandList Start, string PicNum, string? Stength, string? Action, string? Move, string[]? MoveFlag)
        : Structure(Start, CommandList.enda);

    public record ActorCommand(string PicNum, string? Stength, string? Action, string? Move, string[]? MoveFlag)
        : BaseActorCommand(CommandList.actor, PicNum, Stength, Action, Move, MoveFlag);

    public record UserActorCommand(string Type, string PicNum, string? Stength, string? Action, string? Move, string[]? MoveFlag)
        : BaseActorCommand(CommandList.useractor, PicNum, Stength, Action, Move, MoveFlag);

    // ===== Global Settings - Subroutines helpers (ai declare/invoke) =====

    public sealed record AiCommand(string Name, string? Action, string? Move, string[]? MoveFlag)
        : Command(CommandList.ai);

    public sealed record AiInvokeCommand(string Name) : Command(CommandList.ai);

    // ===== Actors - Structures =====

    public sealed record CActorCommand(string Name) : Command(CommandList.cactor);
}
