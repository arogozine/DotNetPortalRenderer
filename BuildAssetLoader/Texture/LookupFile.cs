namespace BuildAssetLoader.Texture
{
    public sealed class LookupFile
    {
        public required byte NumberOfSwaps { get; init; }
        public required byte[][] PaletteSwapTables { get; init; }
    }
}
