namespace BuildAssetLoader.Con
{
    // Flat commands from Commands.Preprocessor.cs and the `define` entry in Commands.cs.
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static Command? TryParsePreprocessor(CommandList command, ConTreeCursor cursor) => command switch
        {
            CommandList.include => new IncludeCommand(cursor.ReadValue()),
            CommandList.includedefault => new IncludedefaultCommand(),
            CommandList.define => new DefineCommand(cursor.ReadValue(), cursor.ReadValue()),

            _ => null,
        };
    }
}
