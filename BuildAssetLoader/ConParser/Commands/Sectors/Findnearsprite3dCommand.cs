namespace BuildAssetLoader.Con
{
    public sealed record Findnearsprite3dCommand(string TileNumber, string Distance, string Gamevar)
        : BaseFindnearCommand(CommandList.findnearsprite3d, TileNumber, Distance, Gamevar);
}

