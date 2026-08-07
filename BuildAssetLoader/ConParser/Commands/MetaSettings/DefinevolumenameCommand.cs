namespace BuildAssetLoader.Con
{
    // definevolumename <episode number> <name>
    public sealed record DefinevolumenameCommand(int EpisodeNumber, string Name) : Command(CommandList.definevolumename);
}

