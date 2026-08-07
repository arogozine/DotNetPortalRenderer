namespace BuildAssetLoader.Con
{
    public sealed record FindnearactorzvarCommand(string TileNumber, string XyDistance, string ZDistance, string Gamevar)
        : BaseFindnearzCommand(CommandList.findnearactorzvar, TileNumber, XyDistance, ZDistance, Gamevar);
}

