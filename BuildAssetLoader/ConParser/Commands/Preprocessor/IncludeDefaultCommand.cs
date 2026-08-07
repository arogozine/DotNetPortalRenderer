namespace BuildAssetLoader.Con
{
    // includedefault — inserts the contents of the default CON file (EDUKE.CON/GAME.CON/NAM.CON/WW2GI.CON).
    public sealed record IncludedefaultCommand() : Command(CommandList.includedefault);
}

