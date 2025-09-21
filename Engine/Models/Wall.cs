
namespace RenderingEngine.Models
{
    internal sealed class Wall
    {
        public bool IntersectsView;
        // Point A
        public float X1;
        public float Y1;
        // Point B
        public float X2;
        public float Y2;

        // Clamped Point A
        public float CX1;
        public float CY1;
        // Clamped Point B
        public float CX2;
        public float CY2;

        //
        public int Neighbor;
        // Plane
        public int XLeft;
        public int XRight;
        public int YLeftCeil;
        public int YLeftFloor;
        public int YRightCeil;
        public int YRightFloor;

        public Wall(float x1, float y1, float x2, float y2, int? neighbor)
        {
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
