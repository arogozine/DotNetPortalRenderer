namespace BuildAssetLoader.Con
{
    public sealed record FindnearactorvarCommand(string TileNumber, string Distance, string Gamevar)
        : BaseFindnearCommand(CommandList.findnearactorvar, TileNumber, Distance, Gamevar);
}

