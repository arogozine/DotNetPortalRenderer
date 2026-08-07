namespace BuildAssetLoader.Con
{
    public sealed record EspawnCommand(string TileNumber) : BaseSpawnCommand(CommandList.espawn, TileNumber);
}

