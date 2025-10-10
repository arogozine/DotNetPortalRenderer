namespace DoomAssetLoader.Map
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Thing
    {
        public readonly short X;

        public readonly short Y;

        public readonly short Angle;

        public readonly ThingType Type;

        public readonly ThingOptions Options;

        public Thing(short x, short y, short angle, short type, ThingOptions options)
        {
            X = x;
            Y = y;
            Angle = angle;
            Type = (ThingType)type;
            Options = options;
        }
    }
}
