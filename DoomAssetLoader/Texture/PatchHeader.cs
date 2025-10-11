namespace DoomAssetLoader.Texture
{
    public sealed class PatchHeader
    {
        public readonly ushort Width;
        public readonly ushort Height;
        public readonly short LeftOffset;
        public readonly short TopOffset;
        public readonly List<Post>[] Columns;

        public PatchHeader(ushort width, ushort height, short leftOffset, short topOffset, List<Post>[] columns)
        {
            Width = width;
            Height = height;
            LeftOffset = leftOffset;
            TopOffset = topOffset;
            Columns = columns;
        }
    }
}
