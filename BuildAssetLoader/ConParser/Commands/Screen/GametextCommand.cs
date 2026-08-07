namespace BuildAssetLoader.Con
{
    // gametext <tilenum> <x> <y> <quote> <shade> <pal> <orientation> <x1> <y1> <x2> <y2>
    public sealed record GametextCommand(
        string TileNum, string X, string Y, int Quote, string Shade, string Pal, string Orientation,
        string X1, string Y1, string X2, string Y2)
        : Command(CommandList.gametext);
}

