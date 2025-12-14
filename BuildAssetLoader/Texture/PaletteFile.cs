namespace BuildAssetLoader.Texture
{
    public class PaletteFile
    {
        public required byte[] Palette { get; init; }
        public required int NumberOfPalLookups { get; init; }
        public required byte[][] PalLookups { get; init; }
        public required byte[] TranslucentLookup { get; init; }
    }
}
