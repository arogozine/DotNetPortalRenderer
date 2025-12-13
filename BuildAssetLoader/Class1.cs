namespace BuildAssetLoader
{
    // https://github.com/jonof/jfbuild/blob/master/doc/buildinf.txt
    // https://moddingwiki.shikadi.net/wiki/MAP_Format_(Build)

    [Flags]
    public enum Stat : short
    {
        Parallaxing = 1,
        Sloped = 2,

        /// <summary>
        /// swap x&y
        /// </summary>
        SwapXy = 4,


        DoubleSmooshiness = 8,
        XFlip = 16,
        YFlip = 32,

        /// <summary>
        /// Align texture to first wall of sector
        /// </summary>
        AlignTexture = 64
    }


    /// <summary>
    /// 40 byte structure
    /// </summary>
    public readonly struct Sector
    {
        /// <summary>
        /// Index to first wall in sector
        /// </summary>
        private readonly short WallPtr;

        /// <summary>
        /// Number of walls in sector
        /// </summary>
        private readonly short WallNum;

        /// <summary>
        /// Z-coordinate (height) of ceiling at first point of sector
        /// </summary>
        private readonly uint CeilingZ;

        /// <summary>
        /// Z-coordinate (height) of floor at first point of sector
        /// </summary>
        private readonly uint FloorZ;

        private readonly Stat CeilingStat;
        private readonly Stat FloorStat;

        /// <summary>
        /// Ceiling texture (index into ART file)
        /// </summary>
        private readonly short CeilingPicNum;

        /// <summary>
        /// Slope value (rise/run; 0 = parallel to floor, 4096 = 45 degrees)
        /// </summary>
        private readonly short CeilingHeiNum;

        /// <summary>
        /// Shade offset
        /// </summary>
        private readonly sbyte CeilingShade;

        /// <summary>
        /// Shade offset
        /// </summary>
        private readonly byte CeilingPal;

        /// <summary>
        /// Palette lookup table number (0 = standard colours)
        /// </summary>
        private readonly byte CeilingXPanning;

        /// <summary>
        /// Texture coordinate X-offset for ceiling
        /// </summary>
        private readonly byte CeilingYPanning;

        /// <summary>
        /// Floor texture (index into ART file)
        /// </summary>
        private readonly short FloorPicNum;

        /// <summary>
        /// Slope value (rise/run; 0 = parallel to floor, 4096 = 45 degrees)
        /// </summary>
        private readonly short FloorHeiNum;

        /// <summary>
        /// Shade offset
        /// </summary>
        private readonly sbyte FloorShade;

        /// <summary>
        /// Palette lookup table number (0 = standard colours)
        /// </summary>
        private readonly byte FloorPal;

        /// <summary>
        /// Texture coordinate X-offset for floor
        /// </summary>
        private readonly byte FloorXPanning;

        /// <summary>
        /// Texture coordinate Y-offset for floor
        /// </summary>
        private readonly byte FloorYPanning;

        /// <summary>
        /// How fast an area changes shade relative to distance
        /// </summary>
        private readonly byte Visibility;

        /// <summary>
        /// Padding byte
        /// </summary>
        private readonly byte Filler;

        /// <summary>
        /// Significance is game-specific (Triggers, etc.)
        /// </summary>
        private readonly short LoTag;

        /// <summary>
        /// Significance is game-specific (Triggers, etc.)
        /// </summary>
        private readonly short HiTag;

        /// <summary>
        /// Significance is game-specific
        /// </summary>
        private readonly short Extra;
    }

    public enum CStat : short
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

        XFlipped = 8,

        MaskingWall =  16,

        OneWayWall = 32,

        /// <summary>
        /// Blocking wall (use with hitscan / cliptype 1)
        /// </summary>
        BlockingWallHitScan = 64,

        Transluscence = 128,

        YFlipped = 256,

        TransluscenceReversing = 521

        /*
	
bit 0: 1 = Blocking wall (use with clipmove, getzrange)
bit 1: 1 = bottoms of invisible walls swapped, 0 = not
bit 2: 1 = align picture on bottom (for doors), 0 = top
bit 3: 1 = x-flipped, 0 = normal
bit 4: 1 = masking wall, 0 = not
bit 5: 1 = 1-way wall, 0 = not
bit 6: 1 = Blocking wall (use with hitscan / cliptype 1)
bit 7: 1 = Transluscence, 0 = not
bit 8: 1 = y-flipped, 0 = normal
bit 9: 1 = Transluscence reversing, 0 = normal
bits 10-15: reserved
         */
    }


    /// <summary>
    /// 32 bytes
    /// </summary>
    public readonly struct Wall
    {
        /// <summary>
        /// X-coordinate of left side of wall (right side coordinate is obtained from the next wall's left side)
        /// </summary>
        public readonly uint X;

        /// <summary>
        /// Y-coordinate of left side of wall (right side coordinate is obtained from the next wall's left side)
        /// </summary>
        public readonly uint Y;

        /// <summary>
        /// Index to next wall on the right (always in the same sector)
        /// </summary>
        public readonly ushort Point2;

        /// <summary>
        /// Index to wall on other side of wall (-1 if there is no sector there)
        /// </summary>
        public readonly short NextWall;

        /// <summary>
        /// Index to sector on other side of wall (-1 if there is no sector)
        /// </summary>
        public readonly short NextSector;

        public readonly CStat CStat;

        /// <summary>
        /// Texture index into ART file
        /// </summary>
        public readonly short PicNum;

        /// <summary>
        /// Texture index into ART file for masked/one-way walls
        /// </summary>
        public readonly short OverPicNum;

        /// <summary>
        /// Shade offset of wall
        /// </summary>
        public readonly sbyte Shade;

        /// <summary>
        /// Palette lookup table number (0 = standard colours)
        /// </summary>
        public readonly byte Pal;

        /// <summary>
        /// Offset for aligning textures
        /// </summary>
        public readonly byte XRepeat;

        /// <summary>
        /// Offset for aligning textures
        /// </summary>
        public readonly byte YRepeat;

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
    }
}
