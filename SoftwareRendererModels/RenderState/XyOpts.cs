namespace SoftwareRendererModels;

[Flags, RenderState]
public enum XyOpts : byte
{
    None = 0,
    FlipX = 1,
    FlipY = 2,
    SwapXY = 4,
    DoubleSize = 8
}