namespace RenderingEngine.DoomMapLoader.Texture
{
    internal readonly struct PatchHeader
    {
        public readonly ushort Width;
        public readonly ushort Height;
        public readonly short LeftOffset;
        public readonly short TopOffset;
        public readonly uint[] ColumnOffsets;

        public PatchHeader(ushort width, ushort height, short leftOffset, short topOffset, uint[] columnOfs)
        {
            Width = width;
            Height = height;
            LeftOffset = leftOffset;
            TopOffset = topOffset;
            ColumnOffsets = columnOfs;
        }
    }
}
