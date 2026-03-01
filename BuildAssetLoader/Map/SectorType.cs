namespace BuildAssetLoader.Map
{
    /// <summary>
    /// 40 byte structure
    /// </summary>
    public readonly struct SectorType
    {
        /// <summary>
        /// Index to first wall in sector
        /// </summary>
        public readonly short WallPtr;

        /// <summary>
        /// Number of walls in sector
        /// </summary>
        public readonly short WallNum;

        /// <summary>
        /// Z-coordinate (height) of ceiling at first point of sector
        /// </summary>
        public readonly int CeilingZ;

        /// <summary>
        /// Z-coordinate (height) of floor at first point of sector
        /// </summary>
        public readonly int FloorZ;

        public readonly Stat CeilingStat;
        public readonly Stat FloorStat;

        /// <summary>
        /// Ceiling texture (index into ART file)
        /// </summary>
        public readonly short CeilingPicNum;

        /// <summary>
        /// Slope value (rise/run; 0 = parallel to floor, 4096 = 45 degrees)
        /// </summary>
        public readonly short CeilingHeiNum;

        /// <summary>
        /// Shade offset
        /// </summary>
        public readonly sbyte CeilingShade;

        /// <summary>
        /// Shade offset
        /// </summary>
        public readonly byte CeilingPal;

        /// <summary>
        /// Palette lookup table number (0 = standard colours)
        /// </summary>
        public readonly byte CeilingXPanning;

        /// <summary>
        /// Texture coordinate X-offset for ceiling
        /// </summary>
        public readonly byte CeilingYPanning;

        /// <summary>
        /// Floor texture (index into ART file)
        /// </summary>
        public readonly short FloorPicNum;

        /// <summary>
        /// Slope value (rise/run; 0 = parallel to floor, 4096 = 45 degrees)
        /// </summary>
        public readonly short FloorHeiNum;

        /// <summary>
        /// Shade offset
        /// </summary>
        public readonly sbyte FloorShade;

        /// <summary>
        /// Palette lookup table number (0 = standard colours)
        /// </summary>
        public readonly byte FloorPal;

        /// <summary>
        /// Texture coordinate X-offset for floor
        /// </summary>
        public readonly byte FloorXPanning;

        /// <summary>
        /// Texture coordinate Y-offset for floor
        /// </summary>
        public readonly byte FloorYPanning;

        /// <summary>
        /// How fast an area changes shade relative to distance
        /// </summary>
        public readonly byte Visibility;

        /// <summary>
        /// Padding byte. Useless byte to make structure aligned.
        /// </summary>
        public readonly byte Filler;

        /// <summary>
        /// Significance is game-specific (Triggers, etc.)
        /// </summary>
        public readonly short LoTag;

        /// <summary>
        /// Significance is game-specific (Triggers, etc.)
        /// </summary>
        public readonly short HiTag;

        /// <summary>
        /// Significance is game-specific
        /// </summary>
        public readonly short Extra;

        public SectorType(short wallPtr, short wallNum, int ceilingZ, int floorZ, Stat ceilingStat, Stat floorStat, short ceilingPicNum, short ceilingHeiNum, sbyte ceilingShade, byte ceilingPal, byte ceilingXPanning, byte ceilingYPanning, short floorPicNum, short floorHeiNum, sbyte floorShade, byte floorPal, byte floorXPanning, byte floorYPanning, byte visibility, byte filler, short loTag, short hiTag, short extra)
        {
            WallPtr = wallPtr;
            WallNum = wallNum;
            CeilingZ = ceilingZ;
            FloorZ = floorZ;
            CeilingStat = ceilingStat;
            FloorStat = floorStat;
            CeilingPicNum = ceilingPicNum;
            CeilingHeiNum = ceilingHeiNum;
            CeilingShade = ceilingShade;
            CeilingPal = ceilingPal;
            CeilingXPanning = ceilingXPanning;
            CeilingYPanning = ceilingYPanning;
            FloorPicNum = floorPicNum;
            FloorHeiNum = floorHeiNum;
            FloorShade = floorShade;
            FloorPal = floorPal;
            FloorXPanning = floorXPanning;
            FloorYPanning = floorYPanning;
            Visibility = visibility;
            Filler = filler;
            LoTag = loTag;
            HiTag = hiTag;
            Extra = extra;
        }
    }
}
