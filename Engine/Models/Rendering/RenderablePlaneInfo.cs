namespace RenderingEngine.Models;

public sealed class RenderablePlaneInfo
{
    public required float WallStartY { get; init; }
    public required float WallEndY { get; init; }
    public required float CeilDistIncr { get; init; }
    public required float FloorDistIncr { get; init; }
    public float? PortalStartY { get; init; }
    public float? PortalEndY { get; init; }

    public float? PortalStartIncr { get; init; }
    public float? PortalEndIncr { get; init; }

}
