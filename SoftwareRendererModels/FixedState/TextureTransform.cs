namespace SoftwareRendererModels;

[Flags, FixedState]
public enum TextureTransform : byte
{
    Normal = 0,
    Rotated = 1,
    FlippedY = 2,
    FlippedX = 4,
    RotatedFlipped = Rotated | FlippedY,
    All = Rotated | FlippedX | FlippedY
}