using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace RenderingEngine.Engine
{
    /// <summary>
    /// For rendering transparent textures on top of a background
    /// </summary>
    internal readonly struct DrawTransparentPixel : IDrawPixel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Draw(uint* surface, uint pixel)
        {
            if (pixel != 0U)
            {
                *surface = pixel;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawLine(uint* surface, Vector256<uint> pixels)
        {
            if (pixels == Vector256<uint>.Zero)
            {
                return;
            }

            if (Avx2.IsSupported)
            {
                Vector256<uint> gtMask = Vector256.GreaterThan(pixels, Vector256<uint>.Zero);
                Avx2.MaskStore(surface, gtMask, pixels);
                return;
            }

            for (int i = 0; i < Vector256<uint>.Count; i++)
            {
                uint pixel = pixels[i];

                if (pixel != 0U)
                {
                    *surface = pixel;
                }

                surface++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawLine(uint* surface, Vector128<uint> pixels)
        {
            if (pixels == Vector128<uint>.Zero)
            {
                return;
            }

            if (Avx2.IsSupported)
            {
                Vector128<uint> gtMask = Vector128.GreaterThan(pixels, Vector128<uint>.Zero);
                Avx2.MaskStore(surface, gtMask, pixels);
                return;
            }

            for (int i = 0; i < Vector128<uint>.Count; i++)
            {
                uint pixel = pixels[i];

                if (pixel != 0U)
                {
                    *surface = pixel;
                }

                surface++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawLine(uint* surface, Vector<uint> pixels)
        {
            if (Vector<uint>.Count == Vector256<uint>.Count)
            {
                DrawLine(surface, pixels.AsVector256());
                return;
            }

            if (Vector<uint>.Count == Vector128<uint>.Count)
            {
                DrawLine(surface, pixels.AsVector128());
                return;
            }
            
            if (pixels == Vector<uint>.Zero)
            {
                return;
            }
            
            for (int i = 0; i < Vector<uint>.Count; i++)
            {
                uint pixel = pixels[i];

                if (pixel != 0U)
                {
                    *surface = pixel;
                }

                surface++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawLine(uint* surface, Vector256<uint> pixels, Vector256<uint> mask)
        {
            mask &= Vector256.GreaterThan(pixels, Vector256<uint>.Zero);

            if (mask == Vector256<uint>.Zero)
            {
                return;
            }

            if (Avx2.IsSupported)
            {
                Avx2.MaskStore(surface, mask, pixels);
                return;
            }

            for (int i = 0; i < Vector256<uint>.Count; i++)
            {
                if (mask[i] != 0U)
                {
                    uint pixel = pixels[i];
                    *surface = pixel;
                }

                surface++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawLine(uint* surface, Vector128<uint> pixels, Vector128<uint> mask)
        {
            mask &= Vector128.GreaterThan(pixels, Vector128<uint>.Zero);

            if (mask == Vector128<uint>.Zero)
            {
                return;
            }

            if (Avx2.IsSupported)
            {
                Avx2.MaskStore(surface, mask, pixels);
                return;
            }

            for (int i = 0; i < Vector128<uint>.Count; i++)
            {
                if (mask[i] != 0U)
                {
                    uint pixel = pixels[i];
                    *surface = pixel;
                }

                surface++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void DrawLine(uint* surface, Vector<uint> pixels, Vector<uint> mask)
        {
            if (pixels == Vector<uint>.Zero)
            {
                return;
            }

            Vector<uint> combinedMask = mask & Vector.GreaterThan(pixels, Vector<uint>.Zero);

            for (int i = 0; i < Vector<uint>.Count; i++)
            {
                if (combinedMask[i] != 0U)
                {
                    uint pixel = pixels[i];
                    *surface = pixel;
                }

                surface++;
            }
        }
        
        
        
        
        
    }
}
