namespace BuildAssetLoader.Con
{
    public sealed record EqspawnvarCommand(string TileNumber) : BaseSpawnCommand(CommandList.eqspawnvar, TileNumber);
}

