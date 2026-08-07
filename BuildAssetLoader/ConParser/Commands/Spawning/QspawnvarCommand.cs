namespace BuildAssetLoader.Con
{
    public sealed record QspawnvarCommand(string TileNumber) : BaseSpawnCommand(CommandList.qspawnvar, TileNumber);
}

