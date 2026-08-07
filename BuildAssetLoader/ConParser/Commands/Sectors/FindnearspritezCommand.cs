namespace BuildAssetLoader.Con
{
    public sealed record FindnearspritezCommand(string TileNumber, string XyDistance, string ZDistance, string Gamevar)
        : BaseFindnearzCommand(CommandList.findnearspritez, TileNumber, XyDistance, ZDistance, Gamevar);
}

