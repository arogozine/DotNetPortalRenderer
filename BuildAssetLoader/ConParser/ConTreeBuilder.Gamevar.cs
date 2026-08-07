namespace BuildAssetLoader.Con
{
    // Flat commands from Commands.Gamevar.cs (Game Variables, Operators, Math, Array Ops, Data Saving).
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static Command? TryParseGamevar(CommandList command, ConTreeCursor cursor) => command switch
        {
            CommandList.GameVar => new GameVarCommand(cursor.ReadValue(), cursor.TryReadInt(), cursor.TryReadInt()),
            CommandList.GameArray => new GameArrayCommand(cursor.ReadValue(), cursor.ReadInt(), cursor.TryReadInt()),

            CommandList.SetVar or CommandList.AddVar or CommandList.SubVar or CommandList.MulVar or CommandList.DivVar
                or CommandList.ModVar or CommandList.AndVar or CommandList.OrVar or CommandList.XorVar or CommandList.RandVar =>
                new VarOpCommand(command, GamevarOperatorLookup.Map[command], cursor.ReadValue(), cursor.ReadValue()),

            CommandList.SetVarVar or CommandList.AddVarVar or CommandList.SubVarVar or CommandList.MulVarVar or CommandList.DivVarVar
                or CommandList.ModVarVar or CommandList.AndVarVar or CommandList.OrVarVar or CommandList.XorVarVar or CommandList.RandVarVar =>
                new VarVarOpCommand(command, GamevarOperatorLookup.Map[command], cursor.ReadValue(), cursor.ReadValue()),

            CommandList.SetArray => ParseSetArray(cursor),

            CommandList.Sqrt => new SqrtCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.CalcHypotenuse => new CalcHypotenuseCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.Sin => new SinCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.Cos => new CosCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.ShiftVarL => new ShiftVarLCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.ShiftVarR => new ShiftVarRCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.MulScale => new MulScaleCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.GetAngle => new GetAngleCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.GetIncAngle => new GetIncAngleCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            CommandList.GetArraySize => new GetArraySizeCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.GetArraySequence => new GetArraySequenceCommand(cursor.ReadValue(), cursor.ReadAllContiguousValues()),
            CommandList.ResizeArray => new ResizeArrayCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.Copy => ParseCopy(cursor),
            CommandList.SetArraySequence => new SetArraySequenceCommand(cursor.ReadValue(), cursor.ReadAllContiguousValues()),

            CommandList.ReadGameVar => new ReadGameVarCommand(cursor.ReadValue()),
            CommandList.SaveGameVar => new SaveGameVarCommand(cursor.ReadValue()),
            CommandList.ReadArrayFromFile => new ReadArrayFromFileCommand(cursor.ReadValue(), cursor.ReadInt()),
            CommandList.WriteArrayToFile => new WriteArrayToFileCommand(cursor.ReadValue(), cursor.ReadInt()),

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
