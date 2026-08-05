namespace BuildAssetLoader.Con
{
    // include "filename.con" — inserts the contents of the specified file as if part of the current file.
    public sealed record IncludeCommand(string Filename) : Command(CommandList.include);

    // includedefault — inserts the contents of the default CON file (EDUKE.CON/GAME.CON/NAM.CON/WW2GI.CON).
    public sealed record IncludedefaultCommand() : Command(CommandList.includedefault);
}
