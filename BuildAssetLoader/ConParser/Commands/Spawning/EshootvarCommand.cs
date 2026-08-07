namespace BuildAssetLoader.Con
{
    public sealed record EshootvarCommand(string TileNumber) : BaseShootCommand(CommandList.eshootvar, TileNumber);
}

