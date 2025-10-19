namespace RenderingEngine.Engine
{
    internal struct RenderWindow
    {
        public bool Calculated;
        public int CeilingStart;
        public int WallStart;
        public int WallEnd;
        public int FloorEnd;
        public float Distance;

        public override readonly string ToString()
        {
            return $"C: {Calculated}, Ceil: {CeilingStart}, WallStart: {WallStart} WallEnd: {WallEnd}, FloorEnd: {FloorEnd}";
        }

    }
}
