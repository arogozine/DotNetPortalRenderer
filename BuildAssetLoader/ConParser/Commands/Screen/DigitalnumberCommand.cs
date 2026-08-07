namespace BuildAssetLoader.Con
{
    // digitalnumber <tilenum> <x> <y> <number> <shade> <pal> <orientation> <x1> <y1> <x2> <y2>
    public sealed record DigitalnumberCommand(
        string TileNum, string X, string Y, string Number, string Shade, string Pal, string Orientation,
        string X1, string Y1, string X2, string Y2)
        : Command(CommandList.digitalnumber);
}

