namespace DoomAssetLoader.Map
{
    /// <summary>
    /// Linedefs contain a two-byte (16 bit) field reserved for various flags. 
    /// </summary>
    [Flags]
    public enum LinedefFlags : short
    {
        /// <summary>
        /// Blocks players and monsters.
        /// </summary>
        Blocking = 1,
        /// <summary>
        /// Blocks monsters.
        /// </summary>
        BlocksMonsters = 2,
        /// <summary>
        /// Has two sides.
        /// </summary>
        TwoSided = 4,
        /// <summary>
        /// Draw lower texture from the bottom.
        /// </summary>
        DontPegTop = 8,
        /// <summary>
        /// Draw upper texture from the top.
        /// </summary>
        DontPegBottom = 16,
        /// <summary>
        /// Show as a wall on the automap, used to hide secret passages.
        /// </summary>
        Secret = 32,
        /// <summary>
        /// Sound can only cross one linedef with this flag (not two).
        /// </summary>
        BlocksSound = 64,
        /// <summary>
        /// Does not appear on the automap.
        /// </summary>
        NotOnMap = 128,
        /// <summary>
        /// Drawn on the automap at the beginning of the level.
        /// </summary>
        AlreadyOnMap = 256
    }
}
