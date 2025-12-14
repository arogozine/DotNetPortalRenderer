namespace BuildAssetLoader.Map
{
    [Flags]
    public enum WallCStat : short
    {
        /// <summary>
        /// Blocking wall (use with clipmove, getzrange)
        /// </summary>
        BlockingWallClipmove = 1,

        /// <summary>
        /// bottoms of invisible walls swapped
        /// </summary>
        BottomsInvisibleWallsSwapped = 2,

        /// <summary>
        /// Align picture on bottom (for doors) 
        /// </summary>
        AlignPictureOnBottom = 4,

        /// <summary>
        /// x-flipped
        /// </summary>
        XFlipped = 8,

        /// <summary>
        /// masking wall
        /// </summary>
        MaskingWall =  16,

        /// <summary>
        /// 1-way wall
        /// </summary>
        OneWayWall = 32,

        /// <summary>
        /// Blocking wall (use with hitscan / cliptype 1)
        /// </summary>
        BlockingWallHitScan = 64,

        /// <summary>
        /// Transluscence reversing
        /// </summary>
        Transluscence = 128,

        /// <summary>
        /// y-flipped
        /// </summary>
        YFlipped = 256,

        /// <summary>
        /// Transluscence reversing,
        /// </summary>
        TransluscenceReversing = 521
    }
}
