namespace BuildAssetLoader.Con
{
    // Flat commands from Commands.Gamevar.cs (Game Variables, Operators, Math, Array Ops, Data Saving).
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static Command? TryParseGamevar(CommandList command, ConTreeCursor cursor) => command switch
        {
            CommandList.gamevar => new GamevarCommand(cursor.ReadValue(), cursor.TryReadInt(), cursor.TryReadInt()),
            CommandList.gamearray => new GamearrayCommand(cursor.ReadValue(), cursor.ReadInt(), cursor.TryReadInt()),

            CommandList.setvar or CommandList.addvar or CommandList.subvar or CommandList.mulvar or CommandList.divvar
                or CommandList.modvar or CommandList.andvar or CommandList.orvar or CommandList.xorvar or CommandList.randvar =>
                new VarOpCommand(command, GamevarOperatorLookup.Map[command], cursor.ReadValue(), cursor.ReadValue()),

            CommandList.setvarvar or CommandList.addvarvar or CommandList.subvarvar or CommandList.mulvarvar or CommandList.divvarvar
                or CommandList.modvarvar or CommandList.andvarvar or CommandList.orvarvar or CommandList.xorvarvar or CommandList.randvarvar =>
                new VarVarOpCommand(command, GamevarOperatorLookup.Map[command], cursor.ReadValue(), cursor.ReadValue()),

            CommandList.setarray => ParseSetArray(cursor),

            CommandList.sqrt => new SqrtCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.calchypotenuse => new CalchypotenuseCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.sin => new SinCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.cos => new CosCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.shiftvarl => new ShiftvarlCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.shiftvarr => new ShiftvarrCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.mulscale => new MulscaleCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.getangle => new GetangleCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.getincangle => new GetincangleCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            CommandList.getarraysize => new GetarraysizeCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.getarraysequence => new GetarraysequenceCommand(cursor.ReadValue(), cursor.ReadAllContiguousValues()),
            CommandList.resizearray => new ResizearrayCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.copy => ParseCopy(cursor),
            CommandList.setarraysequence => new SetarraysequenceCommand(cursor.ReadValue(), cursor.ReadAllContiguousValues()),

            CommandList.readgamevar => new ReadgamevarCommand(cursor.ReadValue()),
            CommandList.savegamevar => new SavegamevarCommand(cursor.ReadValue()),
            CommandList.readarrayfromfile => new ReadarrayfromfileCommand(cursor.ReadValue(), cursor.ReadInt()),
            CommandList.writearraytofile => new WritearraytofileCommand(cursor.ReadValue(), cursor.ReadInt()),

            _ => null,
        };

        private static SetArrayCommand ParseSetArray(ConTreeCursor cursor)
        {
            (string array, string index) = SplitIndexed(cursor.ReadValue());
            return new SetArrayCommand(array, index, cursor.ReadValue());
        }

        private static CopyCommand ParseCopy(ConTreeCursor cursor)
        {
            (string srcArray, string srcIndex) = SplitIndexed(cursor.ReadValue());
            (string dstArray, string dstIndex) = SplitIndexed(cursor.ReadValue());
            return new CopyCommand(srcArray, srcIndex, dstArray, dstIndex, cursor.ReadValue());
        }
    }
}
