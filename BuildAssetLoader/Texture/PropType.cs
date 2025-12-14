namespace BuildAssetLoader.Texture
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct PropType
    {
        public readonly byte AnimType;
        public readonly byte OffsetX;
        public readonly byte OffsetY;
        public readonly byte AnimSpeed;
    }
}
