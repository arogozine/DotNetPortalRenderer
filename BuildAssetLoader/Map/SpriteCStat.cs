namespace BuildAssetLoader.Map
{
    [Flags]
    public enum SpriteCStat : ushort
    {
        /// <summary>
        /// Make sprite blockable
        /// </summary>
        BlockingSpriteClipmove = 1,

        /// <summary>
        /// Make sprite transparent
        /// </summary>
        Translucent = 2,

        /// <summary>
        /// Flip sprite around x-axis
        /// </summary>
        XFlipped = 4,

        /// <summary>
        /// Flip sprite around y-axis
        /// </summary>
        YFlipped = 8,

        /// <summary>
        /// Draw sprite as vertically flat (wall aligned)
        /// </summary>
        Wall = 16,

        /// <summary>
        /// Draw sprite as horizontally flat (floor aligned)
        /// </summary>
        Floor = 32,

        /// <summary>
        /// Make sprite one sided
        /// </summary>
        OneSided = 64,

        /// <summary>
        /// Half submerged
        /// </summary>
        RealCentered = 128,

        /// <summary>
        /// Make sprite able to be hit by weapons
        /// </summary>
        BlockingSpriteHitScan = 256,

        /// <summary>
        /// Second Transparency Level
        /// </summary>
        TransFlip = 512,

        /// <summary>
        /// Sprite will not be forced to take shade of sector
        /// </summary>
        NoShade = 2048,

        /// <summary>
        /// Invisible sprite
        /// </summary>
        Invisible = 32768
    }
}
