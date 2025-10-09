namespace RenderingEngine.DoomMapLoader.Udmf
{
    internal class UdmfLinedef : UdmfObject
    {
        public const string FLAG_BLOCKING = "blocking";
        public const string FLAG_BLOCK_MONSTERS = "blockmonsters";
        public const string FLAG_TWO_SIDED = "twosided";
        public const string FLAG_UNPEG_TOP = "dontpegtop";
        public const string FLAG_UNPEG_BOTTOM = "dontpegbottom";
        public const string FLAG_SECRET = "secret";
        public const string FLAG_BLOCK_SOUND = "blocksound";
        public const string FLAG_DONT_DRAW = "dontdraw";
        public const string FLAG_MAPPED = "mapped";
        public const string ID = "id";
        public const string SPECIAL = "special";
        public const string VERTEX_START = "v1";
        public const string VERTEX_END = "v2";
        public const string SIDEDEF_FRONT = "sidefront";
        public const string SIDEDEF_BACK = "sideback";

        public int? Id => GetValue<int>(ID);

        public int V1 => GetRequiredValue<int>(VERTEX_START);
        public int V2 => GetRequiredValue<int>(VERTEX_END);

        public int? SidedefFront => GetValue<int>(SIDEDEF_FRONT);
        public int? SidedefBack => GetValue<int>(SIDEDEF_BACK);
    }
}
