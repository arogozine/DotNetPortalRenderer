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
            CommandList.GetActorAngle => new GetActorAngleCommand(cursor.ReadValue()),
            CommandList.GetPlayerAngle => new GetPlayerAngleCommand(cursor.ReadValue()),
            CommandList.GetTextureCeiling => new GetTextureCeilingCommand(),
            CommandList.GetTextureFloor => new GetTextureFloorCommand(),
            CommandList.SectGetHitag => new SectGetHitagCommand(),
            CommandList.SectGetLotag => new SectGetLotagCommand(),
            CommandList.SpGetHitag => new SpGetHitagCommand(),
            CommandList.SpGetLotag => new SpGetLotagCommand(),
            CommandList.SetActorAngle => new SetActorAngleCommand(cursor.ReadValue()),
            CommandList.SetPlayerAngle => new SetPlayerAngleCommand(cursor.ReadValue()),

            _ => null,
        };
    }
}
