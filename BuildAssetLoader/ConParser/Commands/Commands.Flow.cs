using System.Collections.Frozen;

namespace BuildAssetLoader.Con
{
    // ===== Global Settings - Procedural (events) =====

    // onevent <EVENT_NAME> ... endevent / appendevent <EVENT_NAME> ... endevent
    public record BaseEventCommand(CommandList Start, string EventName) : Structure(Start, CommandList.endevent);

    public sealed record OneventCommand(string EventName) : BaseEventCommand(CommandList.onevent, EventName);

    public sealed record AppendeventCommand(string EventName) : BaseEventCommand(CommandList.appendevent, EventName);

    // ===== Global Settings - Subroutines (states) =====

    // state <name> ... ends / defstate <name> ... ends / prependstate <name> ... ends / appendstate <name> ... ends
    public record BaseStateCommand(CommandList Start, string Name) : Structure(Start, CommandList.ends);

    public sealed record StateCommand(string Name) : BaseStateCommand(CommandList.state, Name);

    public sealed record DefstateCommand(string Name) : BaseStateCommand(CommandList.defstate, Name);

    // Modifies an existing state by prepending code to its body (analog of onevent, for states).
    public sealed record PrependstateCommand(string Name) : BaseStateCommand(CommandList.prependstate, Name);

    // Modifies an existing state by appending code to its body (analog of appendevent, for states).
    public sealed record AppendstateCommand(string Name) : BaseStateCommand(CommandList.appendstate, Name);

    // state <name> — invocation, runs a previously-defined state's code inline.
    public sealed record StateInvokeCommand(string Name) : Command(CommandList.state);

    // ===== Flow Control - If Components =====

    // nullop — no-op, used in place of empty braces.
    public sealed record NullopCommand() : Command(CommandList.nullop);

    // CommandList.else_ is consumed into ConditionalStructure.ElseBody — no standalone record (see §5).

    // ===== Termination =====

    public sealed record BreakCommand() : Command(CommandList.break_);

    public sealed record ContinueCommand() : Command(CommandList.continue_);

    public sealed record ExitCommand() : Command(CommandList.exit);

    public sealed record ReturnCommand() : Command(CommandList.return_);

    public sealed record TerminateCommand() : Command(CommandList.terminate);

    // ===== Jump (deprecated) =====

    // getcurraddress <addr> — <addr> is a gamevar that receives the current address.
    public sealed record GetcurraddressCommand(string Addr) : Command(CommandList.getcurraddress);

    // jump <address> — <address> is a gamevar previously populated by getcurraddress.
    public sealed record JumpCommand(string Address) : Command(CommandList.jump);

    // ===== Loops =====

    public static class WhileConditionLookup
    {
        public static readonly FrozenDictionary<CommandList, GamevarCondition> Map =
            new Dictionary<CommandList, GamevarCondition>
            {
                [CommandList.whilevarl] = GamevarCondition.Less,
                [CommandList.whilevarvarl] = GamevarCondition.Less,
                [CommandList.whilevare] = GamevarCondition.Equal,
                [CommandList.whilevarn] = GamevarCondition.NotEqual,
                [CommandList.whilevarvarn] = GamevarCondition.NotEqual,
            }.ToFrozenDictionary();
    }

    // whilevarl/whilevare/whilevarn <gamevar> <value> { ... } — second operand is a constant or define/label.
    public sealed record WhileVarCommand(CommandList Start, GamevarCondition Condition, string Gamevar, string Value)
        : LoopStructure(Start);

    // whilevarvarl/whilevarvarn <gamevar> <gamevar> { ... } — both operands are gamevars.
    public sealed record WhileVarVarCommand(CommandList Start, GamevarCondition Condition, string Gamevar, string OtherGamevar)
        : LoopStructure(Start);

    // ===== Move (declare/invoke) =====

    // move <name> <horizontal> <vertical> — declaration, placed outside actor/state code.
    public sealed record MoveCommand(string Name, int Horizontal, int Vertical) : Command(CommandList.move);

    // move <name> <moveflags...> — invocation, used inside actor code.
    public sealed record MoveInvokeCommand(string Name, string[]? MoveFlag) : Command(CommandList.move);
}
