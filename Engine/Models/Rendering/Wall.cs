using RenderingEngine.Engine;

namespace RenderingEngine.Models
{
    internal sealed class Wall : IEquatable<Wall?>
    {
        public Line Line { get; }

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

        public bool IsPortal => Neighbor != EngineConstants.NullSector;

        public Wall(Line line, Point r1, Point r2, int? neighbor)
        {
            Line = line;
            R1 = r1;
            R2 = r2;
            Neighbor = neighbor ?? -1;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Wall);
        }

        public bool Equals(Wall? other)
        {
            return other is not null &&
                   EqualityComparer<Line>.Default.Equals(Line, other.Line) &&
                   IntersectsView == other.IntersectsView &&
                   Flipped == other.Flipped &&
                   R1.Equals(other.R1) &&
                   R2.Equals(other.R2) &&
                   C1.Equals(other.C1) &&
                   C2.Equals(other.C2) &&
                   Neighbor == other.Neighbor &&
                   XLeft == other.XLeft &&
                   XRight == other.XRight &&
                   YLeftCeil == other.YLeftCeil &&
                   YLeftFloor == other.YLeftFloor &&
                   YRightCeil == other.YRightCeil &&
                   YRightFloor == other.YRightFloor &&
                   IsPortal == other.IsPortal;
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(Line);
            hash.Add(IntersectsView);
            hash.Add(Flipped);
            hash.Add(R1);
            hash.Add(R2);
            hash.Add(C1);
            hash.Add(C2);
            hash.Add(Neighbor);
            hash.Add(XLeft);
            hash.Add(XRight);
            hash.Add(YLeftCeil);
            hash.Add(YLeftFloor);
            hash.Add(YRightCeil);
            hash.Add(YRightFloor);
            hash.Add(IsPortal);
            return hash.ToHashCode();
        }

        public static bool operator ==(Wall left, Wall right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Wall left, Wall right)
        {
            return !(left == right);
        }
    }
}
