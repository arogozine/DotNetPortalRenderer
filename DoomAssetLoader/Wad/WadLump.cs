namespace DoomAssetLoader.Wad
{
    public sealed class WadLump
    {
        public string Name { get; private init; }

        public required string? MapName { get; init; }

        public byte[] Bytes { get; private init; }

        public required bool IsMap { get; init; }

        public required bool IsFlat { get; init; }

        public required bool IsPatch { get; init; }

        public required bool IsSprite { get; init; }

        public WadLump(string name, byte[] bytes)
        {
            Name = name;
            Bytes = bytes;
        }
    }
}
