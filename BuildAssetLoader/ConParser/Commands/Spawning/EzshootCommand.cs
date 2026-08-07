namespace BuildAssetLoader.Con
{
    public sealed record EzshootCommand(string Zvel, string TileNumber) : BaseZshootCommand(CommandList.ezshoot, Zvel, TileNumber);
}

