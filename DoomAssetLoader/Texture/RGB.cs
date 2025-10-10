namespace DoomAssetLoader.Texture
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct RGB
    {
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;

        public RGB(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }
    }
}
