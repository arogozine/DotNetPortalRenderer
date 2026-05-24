namespace BuildAssetLoader.Map
{
    /// <summary>
    /// 32 bytes
    /// </summary>
    public readonly struct WallType
    {
        /// <summary>
        /// X-coordinate of left side of wall (right side coordinate is obtained from the next wall's left side)
        /// </summary>
        public readonly int X;

        /// <summary>
        /// Y-coordinate of left side of wall (right side coordinate is obtained from the next wall's left side)
        /// </summary>
        public readonly int Y;

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

        public readonly WallCStat CStat;

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
        /// Palette lookup table number (0 = standard colors)
        /// </summary>
        public readonly byte Pal;

        /// <summary>
        /// Used to change the size of pixels (stretch textures)
        /// </summary>
        public readonly byte XRepeat;

        /// <summary>
        /// Used to change the size of pixels (stretch textures)
        /// </summary>
        public readonly byte YRepeat;

        /// <summary>
        /// Offset for aligning textures
        /// </summary>
        public readonly byte XPanning;

        /// <summary>
        /// Offset for aligning textures
        /// </summary>
        public readonly byte YPanning;

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

        public WallType(int x, int y, ushort point2, short nextWall, short nextSector, WallCStat cStat, short picNum, short overPicNum, sbyte shade, byte pal, byte xRepeat, byte yRepeat, byte xPanning, byte yPanning, short loTag, short hiTag, short extra)
        {
            X = x;
            Y = y;
            Point2 = point2;
            NextWall = nextWall;
            NextSector = nextSector;
            CStat = cStat;
            PicNum = picNum;
            OverPicNum = overPicNum;
            Shade = shade;
            Pal = pal;
            XRepeat = xRepeat;
            YRepeat = yRepeat;
            XPanning = xPanning;
            YPanning = yPanning;
            LoTag = loTag;
            HiTag = hiTag;
            Extra = extra;
        }
    }
}
