namespace BuildAssetLoader.Map
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct StartingPosition
    {
        public readonly int PosX;
        public readonly int PosY;
        // Z coordinates are all shifted up 4
        public readonly int PosZ;
        // All angles are from 0-2047, clockwise
        public readonly ushort Angle;
        // Sector of starting point
        public readonly ushort SectorNumber;
    }
}
