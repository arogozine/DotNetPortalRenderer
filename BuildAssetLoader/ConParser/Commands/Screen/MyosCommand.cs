namespace BuildAssetLoader.Con
{
    // ===== Screen Drawing (deprecated myos family) =====

    // myos <x> <y> <tilenum> <shade> <orientation> — older, more limited rotatesprite; draws at 320x200.
    public sealed record MyosCommand(string X, string Y, string TileNum, string Shade, string Orientation) : Command(CommandList.myos);
}

