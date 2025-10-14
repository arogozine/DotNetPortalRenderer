using RenderingEngine.Models.Json;

namespace RenderingEngine.Models
{
    internal sealed class Wall
    {
        public Line Line { get; }

        public bool IntersectsView { get; set; }
        public bool Flipped { get; set; }

        // Rotated Point A
        public float X1 { get; set; }
        public float Y1 { get; set; }
        // Rotated Point B
        public float X2 { get; set; }
        public float Y2 { get; set; }

        // Clamped Point A
        public float CX1 { get; set; }
        public float CY1 { get; set; }
        // Clamped Point B
        public float CX2 { get; set; }
        public float CY2 { get; set; }

        //
        public int Neighbor { get; set; }
        // Plane
        public int XLeft { get; set; }
        public int XRight { get; set; }
        public int YLeftCeil { get; set; }
        public int YLeftFloor { get; set; }
        public int YRightCeil { get; set; }
        public int YRightFloor { get; set; }

        public Wall(Line line, float x1, float y1, float x2, float y2, int? neighbor)
        {
            Line = line;
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
            Neighbor = neighbor ?? -1;
        }

        public override bool Equals(object? obj)
        {
            return obj is Wall wall &&
                   X1 == wall.X1 &&
                   Y1 == wall.Y1 &&
                   X2 == wall.X2 &&
                   Y2 == wall.Y2 &&
                   CX1 == wall.CX1 &&
                   CY1 == wall.CY1 &&
                   CX2 == wall.CX2 &&
                   CY2 == wall.CY2 &&
                   Neighbor == wall.Neighbor &&
                   XLeft == wall.XLeft &&
                   XRight == wall.XRight &&
                   YLeftCeil == wall.YLeftCeil &&
                   YLeftFloor == wall.YLeftFloor &&
                   YRightCeil == wall.YRightCeil &&
                   YRightFloor == wall.YRightFloor;
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(X1);
            hash.Add(Y1);
            hash.Add(X2);
            hash.Add(Y2);
            hash.Add(CX1);
            hash.Add(CY1);
            hash.Add(CX2);
            hash.Add(CY2);
            hash.Add(Neighbor);
            hash.Add(XLeft);
            hash.Add(XRight);
            hash.Add(YLeftCeil);
            hash.Add(YLeftFloor);
            hash.Add(YRightCeil);
            hash.Add(YRightFloor);
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
