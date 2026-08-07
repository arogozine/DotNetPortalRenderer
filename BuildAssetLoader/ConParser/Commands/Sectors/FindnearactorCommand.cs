namespace BuildAssetLoader.Con
{
    public sealed record FindnearactorCommand(string TileNumber, string Distance, string Gamevar)
        : BaseFindnearCommand(CommandList.findnearactor, TileNumber, Distance, Gamevar);
}

