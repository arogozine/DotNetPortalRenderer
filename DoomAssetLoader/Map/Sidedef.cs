namespace DoomAssetLoader.Map
{
    /// <summary>
    /// A Doom map sidedef.
    /// </summary>
    public readonly struct Sidedef
    {
        /// <summary>
        /// Texture X-offset.
        /// </summary>
		public readonly ushort XOffset;

        /// <summary>
        /// Texture Y-offset.
        /// </summary>
        public readonly ushort YOffset;

        /// <summary>
        /// Upper (above neighboring sector) texture.
        /// </summary>
        public readonly string UpperTexture;

        /// <summary>
        /// Lower (below neighboring sector) texture.
        /// </summary>
        public readonly string LowerTexture;

        /// <summary>
        /// Middle (over neighboring sector, or wall) texture.
        /// </summary>
        public readonly string MiddleTexture;

        /// <summary>
        /// Sector this sidedef faces.
        /// </summary>
        public readonly ushort Sector;

        public readonly string? UpperTextureNullable => UpperTexture == "-" ? null : UpperTexture;

        public readonly string? MiddleTextureNullable => MiddleTexture == "-" ? null : MiddleTexture;

        public readonly string? LowerTextureNullable => LowerTexture == "-" ? null : LowerTexture;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="xOffset">Texture X-offset</param>
        /// <param name="yOffset">Texture Y-offset</param>
        /// <param name="upperTexture">Upper (above neighboring sector) texture</param>
        /// <param name="lowerTexture">Lower (below neighboring sector) texture</param>
        /// <param name="middleTexture">Middle (over neighboring sector, or wall) texture</param>
        /// <param name="sector">Sector this sidedef faces</param>
        public Sidedef(ushort xOffset, ushort yOffset, string upperTexture, string lowerTexture, string middleTexture, ushort sector)
        {
            XOffset = xOffset;
            YOffset = yOffset;
            UpperTexture = upperTexture;
            LowerTexture = lowerTexture;
            MiddleTexture = middleTexture;
            Sector = sector;
        }
	}
}
