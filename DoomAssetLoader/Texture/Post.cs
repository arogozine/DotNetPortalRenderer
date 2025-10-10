namespace DoomAssetLoader.Texture
{
    public readonly struct Post
    {
        /// <summary>
        /// Y offset
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
