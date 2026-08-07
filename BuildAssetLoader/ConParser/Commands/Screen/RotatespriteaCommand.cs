namespace BuildAssetLoader.Con
{
    // rotatespritea <x> <y> <zoom> <ang> <tilenum> <shade> <pal> <orientation> <alpha> <x1> <y1> <x2> <y2>
    public sealed record RotatespriteaCommand(
        string X, string Y, string Zoom, string Ang, string TileNum, string Shade, string Pal, string Orientation, string Alpha,
        string X1, string Y1, string X2, string Y2)
        : Command(CommandList.rotatespritea);
}

