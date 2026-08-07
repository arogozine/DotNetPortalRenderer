namespace BuildAssetLoader.Con
{
    // include "filename.con" — inserts the contents of the specified file as if part of the current file.
    public sealed record IncludeCommand(string Filename) : Command(CommandList.include);
}

