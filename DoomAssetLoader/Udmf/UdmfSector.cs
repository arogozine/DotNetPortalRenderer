namespace DoomAssetLoader.Udmf
{
    public class UdmfSector : UdmfObject
    {
        public const string HEIGHT_FLOOR = "heightfloor";
        public const string HEIGHT_CEILING = "heightceiling";
        public const string TEXTURE_FLOOR = "texturefloor";
        public const string TEXTURE_CEILING = "textureceiling";
        public const string LIGHT_LEVEL = "lightlevel";
        public const string SPECIAL = "special";
        public const string ID = "id";

        public const string XPANNING_FLOOR = "xpanningfloor";
        public const string YPANNING_FLOOR = "ypanningfloor";
        public const string XPANNING_CEILING = "xpanningceiling";
        public const string YPANNING_CEILING = "ypanningceiling";

        public string TextureCeiling => properties[TEXTURE_CEILING];
        public string TextureFloor => properties[TEXTURE_FLOOR];
        public int HeightFloor => GetValue<int>(HEIGHT_FLOOR) ?? 0;
        public int HeightCeiling => GetValue<int>(HEIGHT_CEILING) ?? 0;
        public short LightLevel => GetValue<short>(LIGHT_LEVEL) ?? byte.MaxValue;

        public float? XPanningFloor => GetValue<float>(XPANNING_FLOOR);
        public float? YPanningFloor => GetValue<float>(YPANNING_FLOOR);
        public float? XPanningCeiling => GetValue<float>(XPANNING_CEILING);
        public float? YPanningCeiling => GetValue<float>(YPANNING_CEILING);
    }
}
