namespace RenderingEngine.DoomMapLoader.Udmf
{
    internal class UdmfSidedef : UdmfObject
    {
        public const string OFFSET_X = "offsetx";
        public const string OFFSET_Y = "offsety";
        public const string TEXTURE_TOP = "texturetop";
        public const string TEXTURE_BOTTOM = "texturebottom";
        public const string TEXTURE_MIDDLE = "texturemiddle";
        public const string SECTOR_INDEX = "sector";

        public int? Sector => GetValue<int>(SECTOR_INDEX);

    }
}
