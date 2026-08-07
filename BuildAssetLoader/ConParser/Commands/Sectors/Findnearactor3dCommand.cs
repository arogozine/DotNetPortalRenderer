namespace BuildAssetLoader.Con
{
    public sealed record Findnearactor3dCommand(string TileNumber, string Distance, string Gamevar)
        : BaseFindnearCommand(CommandList.findnearactor3d, TileNumber, Distance, Gamevar);
}

