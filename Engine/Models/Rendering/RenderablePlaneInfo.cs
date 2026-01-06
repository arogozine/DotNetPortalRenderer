namespace RenderingEngine.Models
{
    public readonly ref struct RenderablePlaneInfo
    {
        public readonly float WallStartY;
        public readonly float WallEndY;
        public readonly float CeilDistIncr;
        public readonly float FloorDistIncr;

        public RenderablePlaneInfo(float wallStartY, float wallEndY, float ceilDistIncr, float floorDistIncr)
        {
            WallStartY = wallStartY;
            WallEndY = wallEndY;
            CeilDistIncr = ceilDistIncr;
            FloorDistIncr = floorDistIncr;
        }
    }
}
