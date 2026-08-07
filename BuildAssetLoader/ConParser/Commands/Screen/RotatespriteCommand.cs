namespace BuildAssetLoader.Con
{
    // ===== Screen Drawing =====

    // rotatesprite <x> <y> <zoom> <ang> <tilenum> <shade> <pal> <orientation> <x1> <y1> <x2> <y2>
    public sealed record RotatespriteCommand(
        string X, string Y, string Zoom, string Ang, string TileNum, string Shade, string Pal, string Orientation,
        string X1, string Y1, string X2, string Y2)
        : Command(CommandList.rotatesprite);
}

