namespace DoomAssetLoader.Wad
{
    public static class LumpType
    {
        public const string PlayPal = "PLAYPAL";
        public const string TextMap = "TEXTMAP";
        public const string SideDefs = "SIDEDEFS";
        public const string LineDefs = "LINEDEFS";
        public const string Segs = "SEGS";
        public const string SSectors = "SSECTORS";
        public const string Sectors = "SECTORS";
        public const string Things = "THINGS";
        public const string Other = "OTHER";
        public const string Reject = "REJECT";
        public const string Nodes = "NODES";
        public const string BlockMap = "BLOCKMAP";
        /// <summary>
        /// Lists of wall texture names used in SIDEDEFS lumps
        /// </summary>
        public const string Texture1 = "TEXTURE1";
        /// <summary>
        /// Lists of wall texture names used in SIDEDEFS lumps
        /// </summary>
        public const string Texture2 = "TEXTURE2";

        public const string TXStart = "TX_START";
        public const string TXEnd = "TX_END";

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
        public const string FFStart = "FF_START";

        /// <summary>
        /// Marks the end of the flats
        /// </summary>
        public const string FEnd = "F_END";
        public const string FFEnd = "FF_END";

        public const string ColorMap = "COLORMAP";

        public const string PStart = "P_START";
        public const string PPStart = "PP_START";
        public const string PEnd = "P_END";
        public const string PPEnd = "PP_END";

        public const string SStart = "S_START";
        public const string SSStart = "SS_START";
        public const string SEnd = "S_END";
        public const string SSEnd = "SS_END";
    }
}
