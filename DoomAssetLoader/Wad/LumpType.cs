namespace DoomAssetLoader.Wad
{
    public static class LumpType
    {
        public const string PlayPal = "PLAYPAL";
        public const string TextMap = "TEXTMAP";
        public const string SideDefs = "SIDEDEFS";
        public const string LineDefs = "LINEDEFS";
        public const string Sectors = "SECTORS";
        public const string Things = "THINGS";
        public const string Other = "OTHER";
        /// <summary>
        /// Lists of wall texture names used in SIDEDEFS lumps
        /// </summary>
        public const string Texture1 = "TEXTURE1";
        /// <summary>
        /// Lists of wall texture names used in SIDEDEFS lumps
        /// </summary>
        public const string Texture2 = "TEXTURE2";
        public const string EnDoom = "ENDOOM";
        /// <summary>
        /// Patch names
        /// </summary>
        public const string PNames = "PNAMES";
        public const string Vertexes = "VERTEXES";
        /// <summary>
        /// Marks the beginning of the flats (floor textures)
        /// </summary>
        public const string FStart = "F_START";

        /// <summary>
        /// Marks the end of the flats
        /// </summary>
        public const string FEnd = "F_END";

        public const string ColorMap = "COLORMAP";

        public const string PStart = "P_START";
        public const string PEnd = "P_END";
    }
}
