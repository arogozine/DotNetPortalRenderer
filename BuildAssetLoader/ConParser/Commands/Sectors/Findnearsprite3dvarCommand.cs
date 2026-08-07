namespace BuildAssetLoader.Con
{
    public sealed record Findnearsprite3dvarCommand(string TileNumber, string Distance, string Gamevar)
        : BaseFindnearCommand(CommandList.findnearsprite3dvar, TileNumber, Distance, Gamevar);
}

