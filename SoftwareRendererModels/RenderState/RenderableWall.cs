using System.Diagnostics;
using System.Numerics;

namespace SoftwareRendererModels;

[DebuggerDisplay("Id = {Id}")]
public sealed class RenderableWall : IRenderState, IWallLike
{
    public required Line Line { get; init; }
    public required RenderableSector Sector { get; init; }
    public int? Neighbor => Line.SectorTo;


    public int Id => Line.Id;
    public GameTextureInfo? UpperTexture => Line.UpperTexture;
    public GameTextureInfo? MiddleTexture => Line.MiddleTexture;
    public GameTextureInfo? LowerTexture => Line.LowerTexture;
    public Vector2 PointA => Line.PointA;
    public Vector2 PointB => Line.PointB;
    public int? SectorTo => Line.SectorTo;
    public short? Shade => Line.UpperShade;
    public short? LowerShade => Line.LowerShade;
    public bool TwoSided => Line.TwoSided;
    public bool IsMirror => Line.IsMirror;
    public bool IsPortal => Neighbor.HasValue;
    public bool Traversable => Line.Traversable;


    public bool IntersectsView { get; set; }
    public bool Flipped { get; set; }

    /// <summary>
    /// Rotated PointA
    /// </summary>
    public Vector2 R1 { get; set; }

    /// <summary>
    /// Rotated PointB
    /// </summary>
    public Vector2 R2 { get; set; }

    /// <summary>
    /// Rotated & Clipped PointA
    /// </summary>
    public Vector2 C1 { get; set; }

    /// <summary>
    /// Rotated & Clipped PointB
    /// </summary>
    public Vector2 C2 { get; set; }

    public int XLeft { get; set; }
    public int XRight { get; set; }
    public int YLeftCeil { get; set; }
    public int YLeftFloor { get; set; }
    public int YRightCeil { get; set; }
    public int YRightFloor { get; set; }
    public int YLeftCeilSloped { get; set; }
    public int YLeftFloorSloped { get; set; }
    public int YRightCeilSloped { get; set; }
    public int YRightFloorSloped { get; set; }
    public float Length { get; set; }
    public float AvgDepth { get; set; }
    public int? Bunch { get; set; }


    public int CacheFrame { get; set; } = -1;
    public int CacheMirrorKey { get; set; } = int.MinValue;
    public WallCacheState CacheState { get; set; } = WallCacheState.None;

    public bool HasCachedState(int frame, int mirrorKey, WallCacheState flag) =>
        CacheFrame == frame && CacheMirrorKey == mirrorKey && (CacheState & flag) == flag;

    public void SetCachedState(int frame, int mirrorKey, WallCacheState flag)
    {
        if (CacheFrame != frame || CacheMirrorKey != mirrorKey)
        {
            CacheFrame = frame;
            CacheMirrorKey = mirrorKey;
            CacheState = flag;
        }
        else
        {
            CacheState |= flag;
        }
    }
}
