namespace BuildAssetLoader.Con
{
    public sealed record FindnearspritezvarCommand(string TileNumber, string XyDistance, string ZDistance, string Gamevar)
        : BaseFindnearzCommand(CommandList.findnearspritezvar, TileNumber, XyDistance, ZDistance, Gamevar);
}

