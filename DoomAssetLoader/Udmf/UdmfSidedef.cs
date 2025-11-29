namespace DoomAssetLoader.Udmf
{
    public class UdmfSidedef : UdmfObject
    {
        public const string OFFSET_X = "offsetx";
        public const string OFFSET_Y = "offsety";
        public const string OFFSET_X_TOP = "offsetx_top";
        public const string OFFSET_Y_TOP = "offsety_top";
        public const string OFFSET_X_MID = "offsetx_mid";
        public const string OFFSET_Y_MID = "offsety_mid";
        public const string OFFSET_X_BOTTOM = "offsetx_bottom";
        public const string OFFSET_Y_BOTTOM = "offsety_bottom";
        public const string TEXTURE_TOP = "texturetop";
        public const string TEXTURE_BOTTOM = "texturebottom";
        public const string TEXTURE_MIDDLE = "texturemiddle";
        public const string SECTOR_INDEX = "sector";

        public const string LIGHT = "light";
        public const string LIGHT_TOP = "light_top";
        public const string LIGHT_MID = "light_mid";
        public const string LIGHT_BOTTOM = "light_bottom";
        public const string LIGHTABSOLUTE = "lightabsolute";
        public const string LIGHTABSOLUTE_TOP = "lightabsolute_top";
        public const string LIGHTABSOLUTE_MID = "lightabsolute_mid";
        public const string LIGHTABSOLUTE_BOTTOM = "lightabsolute_bottom";

        public string? TextureTop => this[TEXTURE_TOP];
        public string? TextureMiddle => this[TEXTURE_MIDDLE];
        public string? TextureBottom => this[TEXTURE_BOTTOM];
        public int? Sector => GetValue<int>(SECTOR_INDEX);
        public int? XOffset => GetValue<int>(OFFSET_X);
        public int? YOffset => GetValue<int>(OFFSET_Y);
        public float? XOffsetTop => GetValue<float>(OFFSET_X_TOP);
        public float? YOffsetTop => GetValue<float>(OFFSET_Y_TOP);
        public float? XOffsetMid => GetValue<float>(OFFSET_X_MID);
        public float? YOffsetMid => GetValue<float>(OFFSET_Y_MID);
        public float? XOffsetBottom => GetValue<float>(OFFSET_X_BOTTOM);
        public float? YOffsetBottom => GetValue<float>(OFFSET_Y_BOTTOM);

        public int? Light => GetValue<int>(LIGHT);
        public int? LightTop => GetValue<int>(LIGHT_TOP);
        public int? LightMid => GetValue<int>(LIGHT_MID);
        public int? LightBottom => GetValue<int>(LIGHT_BOTTOM);

        public bool? LightAbsolute => GetValue<bool>(LIGHTABSOLUTE);
        public bool? LightAbsoluteTop => GetValue<bool>(LIGHTABSOLUTE_TOP);
        public bool? LightAbsoluteMid => GetValue<bool>(LIGHTABSOLUTE_MID);
        public bool? LightAbsoluteBottom => GetValue<bool>(LIGHTABSOLUTE_BOTTOM);
    }
}
