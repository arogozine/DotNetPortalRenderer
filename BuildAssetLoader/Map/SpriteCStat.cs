namespace BuildAssetLoader.Map
{
    [Flags]
    public enum SpriteCStat : ushort
    {
        /// <summary>
        /// Blocking sprite (use with clipmove, getzrange)
        /// </summary>
        BlockingSpriteClipmove = 1,

        /// <summary>
        /// Translucence
        /// </summary>
        Translucent = 1 << 1,

        /// <summary>
        /// x-flipped
        /// </summary>
        XFlipped = 1 << 2,

        /// <summary>
        /// y-flipped
        /// </summary>
        YFlipped = 1 << 3,

        /// <summary>
        /// Face sprite (default)
        /// !Wall and !Floor
        /// </summary>
        Face = 0,

        /// <summary>
        /// Wall sprite (like masked walls)
        /// </summary>
        Wall = 1 << 4,

        /// <summary>
        /// Floor sprite (parallel to ceilings & floors)
        /// </summary>
        Floor = 1 << 5,

        /// <summary>
        /// 1-sided sprite
        /// </summary>
        OneSided = 1 << 7,

        /// <summary>
        /// Real centered centering (vs foot center)
        /// </summary>
        RealCentered = 1 << 8,

        /// <summary>
        /// Blocking sprite (use with hitscan / cliptype 1)
        /// </summary>
        BlockingSpriteHitScan = 1 << 9,

        /// <summary>
        /// Reserved bits 9-14
        /// </summary>
        ReservedMask = (0x3F << 9),

        /// <summary>
        /// Invisible sprite
        /// </summary>
        Invisible = 1 << 15
    }
}
