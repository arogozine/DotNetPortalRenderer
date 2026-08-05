using System.Numerics;

namespace Tooling;

public static class Vector2Extensions
{
    extension (Vector2 vector)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out float item1, out float item2)
        {
            item1 = vector.X;
            item2 = vector.Y;
        }
    }
}