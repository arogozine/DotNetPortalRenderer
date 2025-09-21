using System.Runtime.InteropServices;

namespace RenderingEngine.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Point
    {
        public readonly float X;
        public readonly float Y;

        public Point(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object? obj)
        {
            return obj is Point vertex &&
                   X == vertex.X &&
                   Y == vertex.Y;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }
        public static bool operator ==(Point left, Point right)
        {
            return left.X == right.X && left.Y == right.Y;
        }

        public static bool operator !=(Point left, Point right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        public static implicit operator Point((float X, float Y) tuple)
        {
            return new Point(tuple.X, tuple.Y);
        }

        public static implicit operator (float X, float Y)(Point point)
        {
            return (point.X, point.Y);
        }
    }
}
