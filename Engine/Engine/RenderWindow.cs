namespace RenderingEngine.Engine
{
    internal struct RenderWindow
    {
        public bool Calculated;
        public int CeilingStart;
        public int WallStart;
        public int WallEnd;
        public int FloorEnd;

        public override readonly string ToString()
        {
            return $"{Calculated}: {CeilingStart} {WallStart} {WallEnd} {FloorEnd}";
        }

    }
}
