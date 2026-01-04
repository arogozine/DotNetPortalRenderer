namespace BuildAssetLoader.Map
{
    // sizeof(spritetype) = 44
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct SpriteType
    {
        /// <summary>
        /// X-coordinate of sprite
        /// </summary>
        public readonly int X;

        /// <summary>
        /// Y-coordinate of sprite
        /// </summary>
        public readonly int Y;

        /// <summary>
        /// Z-coordinate of a sprite
        /// </summary>
        public readonly int Z;

        public readonly SpriteCStat CStat;

        /// <summary>
        /// Texture index into ART file
        /// </summary>
        public readonly short PicNum;

        /// <summary>
        /// Shade offset of wall
        /// </summary>
        public readonly sbyte Shade;

        /// <summary>
        /// Palette lookup table number (0 = standard colours)
        /// </summary>
        public readonly byte Pal;

        /// <summary>
        /// Size of the movement clipping square (face sprites only)
        /// </summary>
        public readonly byte ClipDist;

        public readonly byte Filler;

        /// <summary>
        /// Change pixel size to stretch/shrink textures
        /// </summary>
        public readonly byte XRepeat;

        /// <summary>
        /// Change pixel size to stretch/shrink textures
        /// </summary>
        public readonly byte YRepeat;

        /// <summary>
        /// Centre sprite animations
        /// </summary>
        public readonly sbyte XOffset;

        /// <summary>
        /// Centre sprite animations
        /// </summary>
        public readonly sbyte YOffset;

        /// <summary>
        /// Current sector of sprite's position
        /// </summary>
        public readonly short SectorNumber;

        /// <summary>
        /// Current status of the sprite (inactive, monster, bullet, etc)
        /// </summary>
        public readonly short StatNumber;

        /// <summary>
        /// Angle the sprite is facing
        /// </summary>
        public readonly ushort Angle;

        public readonly short Owner;
        public readonly short XVel;
        public readonly short YVel;
        public readonly short ZVel;

        /// <summary>
        /// Significance is game-specific
        /// </summary>
        public readonly short LoTag;

        /// <summary>
        /// Significance is game-specific
        /// </summary>
        public readonly short HiTag;

        /// <summary>
        /// Significance is game-specific
        /// </summary>
        public readonly short Extra;
    }
}
