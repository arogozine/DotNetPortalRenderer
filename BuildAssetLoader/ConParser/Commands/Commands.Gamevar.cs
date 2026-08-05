namespace BuildAssetLoader.Con
{
    // ===== Game Variables =====

    // gamevar <varname> <value> <flags> — value/flags may be omitted (defaults to a global var initialized to 0).
    public sealed record GamevarCommand(string Name, int? Value, int? Flags) : Command(CommandList.gamevar);

    // gamearray <name> <size> <flags>
    public sealed record GamearrayCommand(string Name, int Size, int? Flags) : Command(CommandList.gamearray);

    // ===== Gamevar Operators =====
    // setvar/addvar/.../randvar -> VarOpCommand, setvarvar/.../randvarvar -> VarVarOpCommand (see Commands.cs, GamevarOperator, §3.5).

    // setarray <gamearray>[<index>] <gamevar|constant> — indexed array assignment, not a binary var op.
    public sealed record SetArrayCommand(string Array, string Index, string Value) : Command(CommandList.setarray);

    // ===== Math Operations =====

    // sqrt <input variable> <output variable>
    public sealed record SqrtCommand(string Input, string Output) : Command(CommandList.sqrt);

    // calchypotenuse <returnvar> <x> <y>
    public sealed record CalchypotenuseCommand(string ReturnVar, string X, string Y) : Command(CommandList.calchypotenuse);

    // sin <gamevar> <gamevar2> — gamevar = sin(gamevar2), scaled to a hypotenuse of 16384.
    public sealed record SinCommand(string Gamevar, string Angle) : Command(CommandList.sin);

    // cos <gamevar> <gamevar2> — gamevar = cos(gamevar2), scaled to a hypotenuse of 16384.
    public sealed record CosCommand(string Gamevar, string Angle) : Command(CommandList.cos);

    // shiftvarl <gamevar> <number> — left-shift <gamevar> by <number> bits.
    public sealed record ShiftvarlCommand(string Gamevar, string Number) : Command(CommandList.shiftvarl);

    // shiftvarr <gamevar> <number> — right-shift <gamevar> by <number> bits.
    public sealed record ShiftvarrCommand(string Gamevar, string Number) : Command(CommandList.shiftvarr);

    // mulscale <Result> <Factor1> <Factor2> <RightShift> — Result = (Factor1 * Factor2) >> RightShift, computed in 64 bits.
    public sealed record MulscaleCommand(string Result, string Factor1, string Factor2, string RightShift) : Command(CommandList.mulscale);

    // getangle <return> <x> <y> — arctan2(y, x).
    public sealed record GetangleCommand(string Return, string X, string Y) : Command(CommandList.getangle);

    // getincangle <return> <angle1> <angle2> — signed shortest angular difference from angle1 to angle2.
    public sealed record GetincangleCommand(string Return, string Angle1, string Angle2) : Command(CommandList.getincangle);

    // ===== Array Operations =====

    // getarraysize <array name> <return>
    public sealed record GetarraysizeCommand(string ArrayName, string ReturnVar) : Command(CommandList.getarraysize);

    // getarraysequence <gamearray> <gamevar 1> [...] <gamevar N> — up to 32 gamevars.
    public sealed record GetarraysequenceCommand(string Gamearray, string[] Gamevars) : Command(CommandList.getarraysequence);

    // resizearray <array name> <new size>
    public sealed record ResizearrayCommand(string ArrayName, string NewSize) : Command(CommandList.resizearray);

    // copy <src_array>[<src_index>] <dst_array>[<dst_index>] <size>
    public sealed record CopyCommand(string SrcArray, string SrcIndex, string DstArray, string DstIndex, string Size)
        : Command(CommandList.copy);

    // setarraysequence <gamearray> <gamevar 1> [...] <gamevar N> — resizes the array to match the gamevar count.
    public sealed record SetarraysequenceCommand(string Gamearray, string[] Gamevars) : Command(CommandList.setarraysequence);

    // ===== Data Saving =====

    // readgamevar <varname> — restores a gamevar previously saved with savegamevar, from the user's config file.
    public sealed record ReadgamevarCommand(string VarName) : Command(CommandList.readgamevar);

    // savegamevar <varname> — persists a gamevar to the user's config file.
    public sealed record SavegamevarCommand(string VarName) : Command(CommandList.savegamevar);

    // readarrayfromfile <array name> <quote number> — quote holds the file name; resizes the array to fit.
    public sealed record ReadarrayfromfileCommand(string ArrayName, int QuoteNumber) : Command(CommandList.readarrayfromfile);

    // writearraytofile <array name> <quote number> — quote holds the file name.
    public sealed record WritearraytofileCommand(string ArrayName, int QuoteNumber) : Command(CommandList.writearraytofile);
}
