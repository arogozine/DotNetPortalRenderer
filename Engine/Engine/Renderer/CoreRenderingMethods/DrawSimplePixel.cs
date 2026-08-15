using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace RenderingEngine.Engine
{
    internal readonly struct DrawSimplePixel : IDrawPixel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Draw(uint* surface, uint pixel)
        {
            *surface = pixel;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawLine(uint* surface, Vector256<uint> pixels)
        {
            pixels.Store(surface);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawLine(uint* surface, Vector128<uint> pixels)
        {
            pixels.Store(surface);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawLine(uint* surface, Vector<uint> pixels)
        {
            pixels.Store(surface);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawLine(uint* surface, Vector256<uint> pixels, Vector256<uint> mask)
        {
            if (Avx2.IsSupported)
            {
                Avx2.MaskStore(surface, mask, pixels);
                return;
            }

            for (int i = 0; i < Vector256<uint>.Count; i++)
            {
                if (mask[i] != 0U)
                {
                    *surface = pixels[i];
                }

                surface++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawLine(uint* surface, Vector128<uint> pixels, Vector128<uint> mask)
        {
            if (Avx2.IsSupported)
            {
                Avx2.MaskStore(surface, mask, pixels);
                return;
            }

            for (int i = 0; i < Vector128<uint>.Count; i++)
            {
                if (mask[i] != 0U)
                {
                    *surface = pixels[i];
                }

                surface++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawLine(uint* surface, Vector<uint> pixels, Vector<uint> mask)
        {
            if (Vector<uint>.Count == Vector256<uint>.Count)
            {
                DrawLine(surface, pixels.AsVector256(), mask.AsVector256());
                return;
            }

            if (Vector<uint>.Count == Vector128<uint>.Count)
            {
                DrawLine(surface, pixels.AsVector128(), mask.AsVector128());
                return;
            }

            for (int i = 0; i < Vector<uint>.Count; i++)
            {
                if (mask[i] != 0U)
                {
                    *surface = pixels[i];
                }

                surface++;
            }
        }
    }
}
