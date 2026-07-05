using System.Diagnostics;

namespace SoftwareRendererModels;

[DebuggerDisplay("WallStartY = {WallStartY}, WallEndY = {WallEndY}")]
public readonly ref struct RenderablePlaneInfo
{
    public required float WallStartY { get; init; }
    public required float WallEndY { get; init; }
    public required float CeilDistIncr { get; init; }
    public required float FloorDistIncr { get; init; }
    public required float WallStartYSloped { get; init; }
    public required float WallEndYSloped { get; init; }
    public required float CeilDistIncrSloped { get; init; }
    public required float FloorDistIncrSloped { get; init; }

    public float? PortalStartY { get; init; }
    public float? PortalEndY { get; init; }

    public float? PortalStartIncr { get; init; }
    public float? PortalEndIncr { get; init; }

}
