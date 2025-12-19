namespace RenderingEngine.Engine
{
    internal struct RenderWindow
    {
        public RenderColumnStatus Status;
        public int CeilingStart;
        public int WallStart;
        public int WallEnd;
        public int FloorEnd;
        public float Distance;

        public void SetFinished(float distance)
        {
            Status = RenderColumnStatus.FinishedRendering;
            Distance = distance;
        }

        public readonly bool Finished => Status.HasFlag(RenderColumnStatus.FinishedRendering);
        public readonly bool Calculated => Status.HasFlag(RenderColumnStatus.Calculated);
        public readonly bool CanRenderCeiling => Calculated && Status.HasFlag(RenderColumnStatus.CanRenderCeiling);
        public readonly bool CanRenderFloor => Calculated && Status.HasFlag(RenderColumnStatus.CanRenderFloor);
        public readonly bool CanRenderWall => Calculated && Status.HasFlag(RenderColumnStatus.CanRenderWall);
        public readonly bool CanRenderPortal => Calculated && Status.HasFlag(RenderColumnStatus.CanRenderPortal);

        public override readonly string ToString()
        {
            return $"S: {Status}, Ceil: {CeilingStart}, WallStart: {WallStart} WallEnd: {WallEnd}, FloorEnd: {FloorEnd}";
        }
    }
}
