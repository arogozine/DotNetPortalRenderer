namespace BuildAssetLoader.Con
{
    // rotatesprite16 <x> <y> <z> <a> <tilenum> <shade> <pal> <orientation> <x1> <y1> <x2> <y2> — deprecated, 65536x precision.
    public sealed record Rotatesprite16Command(
        string X, string Y, string Z, string A, string TileNum, string Shade, string Pal, string Orientation,
        string X1, string Y1, string X2, string Y2)
        : Command(CommandList.rotatesprite16);
}

