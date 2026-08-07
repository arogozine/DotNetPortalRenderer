namespace BuildAssetLoader.Con
{
    public sealed record QspawnCommand(string TileNumber) : BaseSpawnCommand(CommandList.qspawn, TileNumber);
}

