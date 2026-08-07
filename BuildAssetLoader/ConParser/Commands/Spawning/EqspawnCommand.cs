namespace BuildAssetLoader.Con
{
    public sealed record EqspawnCommand(string TileNumber) : BaseSpawnCommand(CommandList.eqspawn, TileNumber);
}

