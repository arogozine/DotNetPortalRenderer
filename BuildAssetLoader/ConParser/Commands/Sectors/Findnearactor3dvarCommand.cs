namespace BuildAssetLoader.Con
{
    public sealed record Findnearactor3dvarCommand(string TileNumber, string Distance, string Gamevar)
        : BaseFindnearCommand(CommandList.findnearactor3dvar, TileNumber, Distance, Gamevar);
}

