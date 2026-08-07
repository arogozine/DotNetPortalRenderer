namespace BuildAssetLoader.Con
{
    // ===== Gamevar Operators =====
    // setvar/addvar/.../randvar -> VarOpCommand, setvarvar/.../randvarvar -> VarVarOpCommand (see Commands.cs, GamevarOperator, §3.5).

    // setarray <gamearray>[<index>] <gamevar|constant> — indexed array assignment, not a binary var op.
    public sealed record SetArrayCommand(string Array, string Index, string Value) : Command(CommandList.setarray);
}

