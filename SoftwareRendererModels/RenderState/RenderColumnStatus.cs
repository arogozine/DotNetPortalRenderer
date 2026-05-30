namespace SoftwareRendererModels;

[Flags, RenderState]
public enum RenderColumnStatus
{
    Calculated = 1,
    CanRenderWall = 2,
    CanRenderPortal = 4,
    CanRenderFloor = 8,
    CanRenderCeiling = 16,
    FinishedRendering = 32,
    NewRender = CanRenderWall | CanRenderFloor | CanRenderCeiling | CanRenderPortal
}