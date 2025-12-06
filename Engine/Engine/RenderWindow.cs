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

        public void SetFinished(float distance)
        {
            Calculated = false;

            Distance = distance;
            CeilingStart = 0;
            WallStart = 0;
            WallEnd = 0;
            FloorEnd = 0;
        }

        public readonly bool CanRender => CanRenderCeiling || CanRenderFloor || CanRenderWall;
        public readonly bool CanRenderCeiling => Calculated && CeilingStart < WallStart;
        public readonly bool CanRenderFloor => Calculated && WallEnd < FloorEnd;
        public readonly bool CanRenderWall => Calculated && WallStart < WallEnd;
        public readonly bool CanRenderMiddleWall => Calculated && FloorEnd <= CeilingStart;

        public override readonly string ToString()
        {
            return $"C: {Calculated}, Ceil: {CeilingStart}, WallStart: {WallStart} WallEnd: {WallEnd}, FloorEnd: {FloorEnd}";
        }
    }
}
