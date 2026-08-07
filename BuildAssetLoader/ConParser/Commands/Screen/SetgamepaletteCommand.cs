namespace BuildAssetLoader.Con
{
    // setgamepalette <pal_ID> — deprecated; switches between LOOKUP.DAT base palettes (0-6).
    public sealed record SetgamepaletteCommand(string PalId) : Command(CommandList.setgamepalette);
}

