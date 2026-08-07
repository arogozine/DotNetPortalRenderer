namespace BuildAssetLoader.Con
{
    // myospalx <x> <y> <tilenum> <shade> <orientation> <pal> — like myospal, but drawn at half size.
    public sealed record MyospalxCommand(string X, string Y, string TileNum, string Shade, string Orientation, string Pal)
        : Command(CommandList.myospalx);
}

