namespace SoftwareRendererModels;

[Flags, FixedState]
public enum TextureRenderingOptions
{
    None = 0,
    FromTop = 1,
    FromSectorTop = 2,
    FromBottom = 4,
    FromSectorBottom = 8,
    Skybox = 16,
    FlipX = 32,
    FlipY = 64,
    SwapXY = 128,
    AlignWithFirstWall = 256,
    Sloped = 512,
    Translucent = 1024,
    FromLower = 2048
}