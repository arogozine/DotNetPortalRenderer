namespace RenderingEngine.DoomMapLoader.Udmf
{
    internal class UdmfSector : UdmfObject
    {
        public const string HEIGHT_FLOOR = "heightfloor";
        public const string HEIGHT_CEILING = "heightceiling";
        public const string TEXTURE_FLOOR = "texturefloor";
        public const string TEXTURE_CEILING = "textureceiling";
        public const string LIGHT_LEVEL = "lightlevel";
        public const string SPECIAL = "special";
        public const string ID = "id";

        public int? HeightFloor => GetValue<int>(HEIGHT_FLOOR);
        public int? HeightCeiling => GetValue<int>(HEIGHT_CEILING);
    }
}
