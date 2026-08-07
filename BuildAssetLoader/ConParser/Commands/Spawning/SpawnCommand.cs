namespace BuildAssetLoader.Con
{
    public sealed record SpawnCommand(string TileNumber) : BaseSpawnCommand(CommandList.spawn, TileNumber);
}

