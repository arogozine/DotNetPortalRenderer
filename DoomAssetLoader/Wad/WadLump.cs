namespace DoomAssetLoader.Wad
{
    public sealed class WadLump
    {
        public string Name { get; private init; }

        public byte[] Bytes { get; private init; }

        public bool IsFlat { get; private init; }

        public bool IsPatch { get; private init; }


        public WadLump(string name, byte[] bytes, bool isFlat, bool isPatch)
        {
            Name = name;
            Bytes = bytes;
            IsFlat = isFlat;
            IsPatch = isPatch;
        }
    }
}
