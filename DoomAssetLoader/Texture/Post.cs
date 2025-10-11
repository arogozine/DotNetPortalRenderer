namespace DoomAssetLoader.Texture
{
    /// <summary>
    /// Column of pixels going downwards
    /// </summary>
    public sealed class Post
    {
        /// <summary>
        /// Row to begin drawing this post at. 0 means whatever height the PatchHeader (TopOffset)
        /// </summary>
        public readonly byte TopDelta;
        public readonly byte Length;
        public readonly byte[] Data;

        public Post(byte topDelta, byte length, byte[] data)
        {
            TopDelta = topDelta;
            Length = length;
            Data = data;
        }
    }
}
