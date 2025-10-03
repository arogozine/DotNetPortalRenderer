namespace RenderingEngine.DoomMapLoader.Wad
{
    internal sealed class WadLump
    {
        public string Name { get; init; }

        public byte[] Bytes { get; init; }

        public WadLump(string name, byte[] bytes)
        {
            Name = name;
            Bytes = bytes;
        }
    }
}
