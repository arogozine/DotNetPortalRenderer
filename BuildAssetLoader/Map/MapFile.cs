namespace BuildAssetLoader.Map
{
    public sealed class MapFile
    {
        private readonly byte[] _binary;
        private readonly (int start, int length) _sectors;
        private readonly (int start, int length) _walls;
        private readonly (int start, int length) _sprites;

        public MapFile(
            byte[] binary,
            (int start, int length) sectors,
            (int start, int length) walls,
            (int start, int length) sprites
            )
        {
            _binary = binary;
            _sectors = sectors;
            _walls = walls;
            _sprites = sprites;
        }

        public required string MapName { get; init; }
        public required uint Version { get; init; }
        public required StartingPosition StartingPosition { get; init; }

        public Span<SectorType> Sectors => GetSpan<SectorType>(_sectors);
        public Span<WallType> Walls => GetSpan<WallType>(_walls);
        public Span<SpriteType> Sprites => GetSpan<SpriteType>(_sprites);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Span<T> GetSpan<T>((int Start, int Length) slice)
            where T : struct
        {
            // we re-interpret the existing binary instead of allocating something new
            return MemoryMarshal.Cast<byte, T>(_binary.AsSpan().Slice(slice.Start, slice.Length));
        }
    }
}
