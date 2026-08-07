namespace BuildAssetLoader.Con
{
    // digitalnumberz <tilenum> <x> <y> <number> <shade> <pal> <orientation> <x1> <y1> <x2> <y2> <digitalscale>
    public sealed record DigitalnumberzCommand(
        string TileNum, string X, string Y, string Number, string Shade, string Pal, string Orientation,
        string X1, string Y1, string X2, string Y2, string DigitalScale)
        : Command(CommandList.digitalnumberz);
}

