namespace BuildAssetLoader.Con
{
    public sealed record EspawnvarCommand(string TileNumber) : BaseSpawnCommand(CommandList.espawnvar, TileNumber);
}

