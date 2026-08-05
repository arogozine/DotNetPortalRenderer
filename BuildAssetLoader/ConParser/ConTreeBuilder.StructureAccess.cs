namespace BuildAssetLoader.Con
{
    // Flat commands from Commands.StructureAccess.cs (deprecated shortcuts only).
    // The get&lt;struct&gt;[id].member / set&lt;struct&gt;[id].member family is not tokenized as a CommandToken
    // (the brackets glue onto the keyword with no whitespace) and is instead handled by
    // ConTreeBuilder.ParseStructureAccessFallback, which decodes the raw ValueToken text directly.
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static Command? TryParseStructureAccess(CommandList command, ConTreeCursor cursor) => command switch
        {
            CommandList.getactorangle => new GetactorangleCommand(cursor.ReadValue()),
            CommandList.getplayerangle => new GetplayerangleCommand(cursor.ReadValue()),
            CommandList.gettextureceiling => new GettextureceilingCommand(),
            CommandList.gettexturefloor => new GettexturefloorCommand(),
            CommandList.sectgethitag => new SectgethitagCommand(),
            CommandList.sectgetlotag => new SectgetlotagCommand(),
            CommandList.spgethitag => new SpgethitagCommand(),
            CommandList.spgetlotag => new SpgetlotagCommand(),
            CommandList.setactorangle => new SetactorangleCommand(cursor.ReadValue()),
            CommandList.setplayerangle => new SetplayerangleCommand(cursor.ReadValue()),

            _ => null,
        };
    }
}
