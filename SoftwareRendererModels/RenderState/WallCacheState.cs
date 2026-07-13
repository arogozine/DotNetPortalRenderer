namespace SoftwareRendererModels;

[Flags, RenderState]
public enum WallCacheState : byte
{
    None = 0,
    Rotated = 1,
    PlaneCalculated = 2,
}
