namespace BuildAssetLoader.Map
{
    [Flags]
    public enum WallCStat : short
    {
        /// <summary>
        /// Make wall blockable
        /// </summary>
        BlockingWallClipmove = 1,

        /// <summary>
        /// Make bottoms of invisible walls swapped
        /// </summary>
        BottomsInvisibleWallsSwapped = 2,

        /// <summary>
        /// Align picture on bottom (for doors) 
        /// </summary>
        AlignPictureOnBottom = 4,

        /// <summary>
        /// Flip wall around x-axis
        /// </summary>
        XFlipped = 8,

        /// <summary>
        /// Make wall masking, one-sided. However it also disables transparency on the masked wall.
        /// </summary>
        MaskingWall =  16,

        /// <summary>
        /// 1-way wall
        /// </summary>
        OneWayWall = 32,

        /// <summary>
        /// Make wall able to be hit by weapons
        /// </summary>
        BlockingWallHitScan = 64,

        /// <summary>
        /// Make wall transparent
        /// </summary>
        Transluscence = 128,

        /// <summary>
        /// Flip wall around y-axis
        /// </summary>
        YFlipped = 256,

        /// <summary>
        /// Second transparency level (combine with cstat 128)
        /// </summary>
        TransluscenceReversing = 512,

        YaxUpwall = 1024,

        YaxDownwall = 2048,

        /// <summary>
        /// Rotate texture by 90 degrees counter-clockwise. (3D-mode hotkey 'R' in mapster32)
        /// </summary>
        Rotate90 = 4096,
    }
}
