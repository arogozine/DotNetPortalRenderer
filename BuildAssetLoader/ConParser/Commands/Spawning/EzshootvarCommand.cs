namespace BuildAssetLoader.Con
{
    public sealed record EzshootvarCommand(string Zvel, string TileNumber) : BaseZshootCommand(CommandList.ezshootvar, Zvel, TileNumber);
}

