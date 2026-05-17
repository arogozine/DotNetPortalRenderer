using RenderingEngine.Engine;
using RenderingEngine.Models.Rendering;

namespace RenderingEngine.Models
{
    internal sealed class RenderableWall : IEquatable<RenderableWall?>, IWallLike
    {
        public int Id => Line.Id;
        public TextureInfo? UpperTexture => Line.UpperTexture;
        public TextureInfo? MiddleTexture => Line.MiddleTexture;
        public TextureInfo? LowerTexture => Line.LowerTexture;
        public Point PointA => Line.PointA;
        public Point PointB => Line.PointB;
        public int? SectorTo => Line.SectorTo;
        public short Shade => Line.Shade;
        public bool TwoSided => Line.TwoSided;
        public bool IsMirror => Line.IsMirror;

        public Line Line { get; }

        public Sector Sector { get; }

        public bool IntersectsView { get; set; }
        public bool Flipped { get; set; }

        public Point R1 { get; set; }
        public Point R2 { get; set; }

        public Point C1 { get; set; }
        public Point C2 { get; set; }



        //
        public int Neighbor { get; }
        // Plane
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
        public int Bunch { get; set; } = EngineConstants.Unset;

        public bool IsPortal => Neighbor != EngineConstants.NullSector;

        public RenderableWall(Line line, Point r1, Point r2, Sector sector, int? neighbor)
        {
            Line = line;
            R1 = r1;
            R2 = r2;
            Sector = sector;
            Neighbor = neighbor ?? -1;
        }

        public override bool Equals(object? obj) => Equals(obj as RenderableWall);
        public bool Equals(RenderableWall? other) => other?.Id == Id;
        public override int GetHashCode() => Id.GetHashCode();
        public static bool operator ==(RenderableWall left, RenderableWall right) => left.Id == right.Id;
        public static bool operator !=(RenderableWall left, RenderableWall right) => left.Id != right.Id;
    }
}
