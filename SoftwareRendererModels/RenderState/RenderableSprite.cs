using System.Diagnostics;
using System.Numerics;

namespace SoftwareRendererModels;

[DebuggerDisplay("Id = {Id}, Texture = {Texture.Name}")]
public abstract class RenderableSprite : IRenderState
{
    public required Sprite Sprite { get; set; }

    public int Id => Sprite.Id;
    public int SectorId => Sprite.SectorId;
    public Vector2 Location => Sprite.Location;
    public Vector2 PointA => Sprite.PointA;
    public Vector2 PointB => Sprite.PointB;
    public float Angle => Sprite.Angle;
    public float Height => Sprite.Height;
    public GameTextureInfo Texture => Sprite.Texture;
    public float Length => Sprite.Length;
    public short? Shade => Sprite.Shade;

    public float AngleToPlayer { get; set; }

    public bool IntersectsView { get; set; }
    public Vector2 Rotated { get; set; }

    public Vector2 R1 { get; set; }
    public Vector2 R2 { get; set; }

    public float DistanceMax { get; set; }
    public float DistanceMin { get; set; }
    public bool Flipped { get; set; }
    public int XLeft { get; set; }
    public int XRight { get; set; }
}
