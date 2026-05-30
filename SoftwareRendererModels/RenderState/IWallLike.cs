using System.Numerics;

namespace SoftwareRendererModels;

public interface IWallLike
{
    public Vector2 R1 { get; }
    public Vector2 R2 { get; }
    public bool IntersectsView { get; }
    public float Length { get; }
    public bool Flipped { get; }
    public int XLeft { get; }
    public int XRight { get; }
    public int YLeftCeil { get; }
    public int YLeftFloor { get; }
    public int YRightCeil { get; }
    public int YRightFloor { get; }
    public short? Shade { get; }
}
