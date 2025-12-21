namespace RenderingEngine.Models
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct Point : IEquatable<Point>
    {
        [FieldOffset(0)]
        public readonly ulong Value;
        [FieldOffset(0)]
        public readonly float X;
        [FieldOffset(4)]
        public readonly float Y;

        public Point(float x, float y)
        {
            X = x;
            Y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj)
        {
            return obj is Point vertex && Value == vertex.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => Value.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Point left, Point right)
        {
            return left.Value == right.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Point left, Point right)
        {
            return left.Value != right.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"({X}, {Y})";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Point other)
        {
            return Value == other.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Point((float X, float Y) tuple)
        {
            return new Point(tuple.X, tuple.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out float item1, out float item2)
        {
            item1 = X;
            item2 = Y;
        }
    }
}
