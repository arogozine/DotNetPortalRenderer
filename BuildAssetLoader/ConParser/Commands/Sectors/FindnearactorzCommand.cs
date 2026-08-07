namespace BuildAssetLoader.Con
{
    public sealed record FindnearactorzCommand(string TileNumber, string XyDistance, string ZDistance, string Gamevar)
        : BaseFindnearzCommand(CommandList.findnearactorz, TileNumber, XyDistance, ZDistance, Gamevar);
}

