namespace BuildAssetLoader.Texture
{
    public sealed class PaletteFile
    {
        public required byte[] Palette { get; init; }
        public required int NumberOfPalLookups { get; init; }
        public required byte[][] PalLookups { get; init; } // TODO: Shade
        public required byte[] TranslucentLookup { get; init; }
    }
}
