namespace BuildAssetLoader.Con
{
    public sealed record EshootCommand(string TileNumber) : BaseShootCommand(CommandList.eshoot, TileNumber);
}

