namespace BuildAssetLoader.Texture
{
    public sealed class TileType
    {
        private readonly byte[] _binary;
        private readonly (int Start, int Length) _pixels;

        public short XSize { get; }
        public short YSize { get; }
        public PropType Properties { get; }
        public Span<byte> Pixels => _binary.AsSpan().Slice(_pixels.Start, _pixels.Length);

        public TileType(
            byte[] binary,
            (int Start, int Length) pixels,
            short xSize,
            short ySize,
            PropType properties)
        {
            _binary = binary;
            _pixels = pixels;
            XSize = xSize;
            YSize = ySize;
            Properties = properties;
        }
    }
}
