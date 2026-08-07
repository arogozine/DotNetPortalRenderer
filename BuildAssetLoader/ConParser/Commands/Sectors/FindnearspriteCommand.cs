namespace BuildAssetLoader.Con
{
    public sealed record FindnearspriteCommand(string TileNumber, string Distance, string Gamevar)
        : BaseFindnearCommand(CommandList.findnearsprite, TileNumber, Distance, Gamevar);
}

