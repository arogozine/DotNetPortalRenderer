namespace BuildAssetLoader.Con
{
    // myosx <x> <y> <tilenum> <shade> <orientation> — like myos, but drawn at half size.
    public sealed record MyosxCommand(string X, string Y, string TileNum, string Shade, string Orientation) : Command(CommandList.myosx);
}

