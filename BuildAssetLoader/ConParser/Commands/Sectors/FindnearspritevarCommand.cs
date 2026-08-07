namespace BuildAssetLoader.Con
{
    public sealed record FindnearspritevarCommand(string TileNumber, string Distance, string Gamevar)
        : BaseFindnearCommand(CommandList.findnearspritevar, TileNumber, Distance, Gamevar);
}

