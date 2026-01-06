namespace RenderingEngine.Engine
{
    [Flags]
    public enum RenderColumnStatus : byte
    {
        Calculated = 1,
        CanRenderWall = 2,
        CanRenderPortal = 4,
        CanRenderFloor = 8,
        CanRenderCeiling = 16,
        FinishedRendering = 32,
        NewRender = CanRenderWall | CanRenderFloor | CanRenderCeiling | CanRenderPortal
    }

    internal static class RenderColumnStatusExtensions
    {
        extension (RenderColumnStatus status)
        {
            public bool IsFinished => status.HasFlag(RenderColumnStatus.FinishedRendering);
            public bool IsCalculated => status.HasFlag(RenderColumnStatus.Calculated);
            public bool CeilingRenderable => status.HasFlag(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderCeiling);
            public bool FloorRenderable => status.HasFlag(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderFloor);
            public bool WallRenderable => status.HasFlag(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderWall);
            public bool PortalRenderable => status.HasFlag(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderPortal);
        }
    }
}
