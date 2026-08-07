namespace BuildAssetLoader.Con
{
    // myospal <x> <y> <tilenum> <shade> <orientation> <pal> — like myos, with an explicit palette.
    public sealed record MyospalCommand(string X, string Y, string TileNum, string Shade, string Orientation, string Pal)
        : Command(CommandList.myospal);
}

