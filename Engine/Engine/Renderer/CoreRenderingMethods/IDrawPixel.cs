using System.Numerics;
using System.Runtime.Intrinsics;

namespace RenderingEngine.Engine
{
    internal unsafe interface IDrawPixel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static abstract void Draw(uint* surface, uint pixels);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static abstract void DrawLine(uint* surface, Vector256<uint> pixels);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static abstract void DrawLine(uint* surface, Vector128<uint> pixels);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static abstract void DrawLine(uint* surface, Vector<uint> pixels);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static abstract void DrawLine(uint* surface, Vector256<uint> pixels, Vector256<uint> mask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static abstract void DrawLine(uint* surface, Vector128<uint> pixels, Vector128<uint> mask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static abstract void DrawLine(uint* surface, Vector<uint> pixels, Vector<uint> mask);
    }
}
