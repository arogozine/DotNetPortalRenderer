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
        public readonly bool CanRenderCeiling => Status.HasFlag(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderCeiling);
        public readonly bool CanRenderFloor => Status.HasFlag(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderFloor);
        public readonly bool CanRenderWall => Status.HasFlag(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderWall);
        public readonly bool CanRenderPortal => Status.HasFlag(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderPortal);

        public override readonly string ToString()
        {
            return $"S: {Status}, Ceil: {CeilingStart}, WallStart: {WallStart} WallEnd: {WallEnd}, FloorEnd: {FloorEnd}";
        }
    }
}
