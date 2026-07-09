namespace BuildAssetLoader.Map
{
    [Flags]
    public enum Stat : short
    {
        Parallaxing = 1,
        Sloped = 2,

        /// <summary>
        /// swap x &amp; y
        /// </summary>
        SwapXy = 4,


        DoubleSmooshiness = 8,
        XFlip = 16,
        YFlip = 32,

        /// <summary>
        /// Align texture to first wall of sector.
        /// Texture relativity ("R" key)
        /// </summary>
        AlignTexture = 64,

        /// <summary>
        /// Masked
        /// </summary>
        Masked = 128,

        /// <summary>
        /// Translucent mask
        /// </summary>
        Transluscent = 256
    }
}
