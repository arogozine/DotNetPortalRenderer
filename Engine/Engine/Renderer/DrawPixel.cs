using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace RenderingEngine.Engine
{
    internal unsafe interface IDrawPixel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Draw(uint* surface, uint pixels);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void DrawLine(uint* surface, Vector256<uint> pixels);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void DrawLine(uint* surface, Vector128<uint> pixels);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void DrawLine(uint* surface, Vector<uint> pixels);
    }

    internal readonly ref struct DrawSimplePixel : IDrawPixel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void Draw(uint* surface, uint pixel)
        {
            *surface = pixel;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void DrawLine(uint* surface, Vector256<uint> pixels)
        {
            pixels.Store(surface);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void DrawLine(uint* surface, Vector128<uint> pixels)
        {
            pixels.Store(surface);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void DrawLine(uint* surface, Vector<uint> pixels)
        {
            pixels.Store(surface);
        }
    }

    internal readonly ref struct DrawTransparentPixel : IDrawPixel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void Draw(uint* surface, uint pixel)
        {
            if (pixel != 0U)
            {
                *surface = pixel;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void DrawLine(uint* surface, Vector256<uint> pixels)
        {
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
        public readonly unsafe void DrawLine(uint* surface, Vector128<uint> pixels)
        {
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
        public readonly unsafe void DrawLine(uint* surface, Vector<uint> pixels)
        {
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
    }

    internal readonly ref struct DrawAlphaPixel : IDrawPixel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void Draw(uint* surface, uint pixel)
        {
            if (pixel != 0U)
            {
                *surface = BlendBGRA(*surface, pixel);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void DrawLine(uint* surface, Vector256<uint> pixels)
        {
            pixels = BlendBGRA(Vector256.Load(surface), pixels);
            Vector256.Store(pixels, surface);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void DrawLine(uint* surface, Vector128<uint> pixels)
        {
            pixels = BlendBGRA(Vector128.Load(surface), pixels);
            Vector128.Store(pixels, surface);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly unsafe void DrawLine(uint* surface, Vector<uint> pixels)
        {
            pixels = BlendBGRA(Vector.Load(surface), pixels);
            Vector.Store(pixels, surface);
        }

        static uint BlendBGRA(uint bgraDstU, uint bgraSrcU)
        {
            const uint a = 127;
            const uint ByteMask = 0xFF;
            const uint Alpha = (uint)byte.MaxValue << 24;

            uint bDst = bgraDstU & ByteMask;
            uint gDst = (bgraDstU >> 8) & ByteMask;
            uint rDst = (bgraDstU >> 16) & ByteMask;

            uint bSrc = bgraSrcU & ByteMask;
            uint gSrc = (bgraSrcU >> 8) & ByteMask;
            uint rSrc = (bgraSrcU >> 16) & ByteMask;

            uint bOut = (bSrc * a + bDst * a) >> 8;
            uint gOut = (gSrc * a + gDst * a) >> 8;
            uint rOut = (rSrc * a + rDst * a) >> 8;

            return (Alpha | (rOut << 16) | (gOut << 8) | bOut);
        }

        static Vector128<uint> BlendBGRA(Vector128<uint> bgraDst, Vector128<uint> bgraSrc)
        {
            Vector128<uint> gMask = Vector128.GreaterThan(bgraSrc, Vector128<uint>.Zero);
            bgraSrc = Vector128.ConditionalSelect(gMask, bgraSrc, bgraDst);

            Vector128<uint> a = Vector128.Create((uint)127);
            Vector128<uint> byteMask = Vector128.Create((uint)0xFF);
            Vector128<uint> alpha = Vector128.Create((uint)byte.MaxValue << 24);

            Vector128<uint> bDst = bgraDst & byteMask;
            Vector128<uint> bSrc = bgraSrc & byteMask;

            Vector128<uint> gDst = (bgraDst >> 8) & byteMask;
            Vector128<uint> gSrc = (bgraSrc >> 8) & byteMask;

            Vector128<uint> rDst = (bgraDst >> 16) & byteMask;
            Vector128<uint> rSrc = (bgraSrc >> 16) & byteMask;

            Vector128<uint> bOut = ((bSrc * a) + (bDst * a)) >> 8;
            Vector128<uint> gOut = ((gSrc * a) + (gDst * a)) >> 8;
            Vector128<uint> rOut = ((rSrc * a) + (rDst * a)) >> 8;

            return alpha | (rOut << 16) | (gOut << 8) | bOut;
        }

        static Vector256<uint> BlendBGRA(Vector256<uint> bgraDst, Vector256<uint> bgraSrc)
        {
            Vector256<uint> gMask = Vector256.GreaterThan(bgraSrc, Vector256<uint>.Zero);
            bgraSrc = Vector256.ConditionalSelect(gMask, bgraSrc, bgraDst);

            Vector256<uint> a = Vector256.Create((uint)127);
            Vector256<uint> byteMask = Vector256.Create((uint)0xFF);
            Vector256<uint> alpha = Vector256.Create((uint)byte.MaxValue << 24);

            Vector256<uint> bDst = bgraDst & byteMask;
            Vector256<uint> bSrc = bgraSrc & byteMask;

            Vector256<uint> gDst = (bgraDst >> 8) & byteMask;
            Vector256<uint> gSrc = (bgraSrc >> 8) & byteMask;

            Vector256<uint> rDst = (bgraDst >> 16) & byteMask;
            Vector256<uint> rSrc = (bgraSrc >> 16) & byteMask;

            Vector256<uint> bOut = ((bSrc * a) + (bDst * a)) >> 8;
            Vector256<uint> gOut = ((gSrc * a) + (gDst * a)) >> 8;
            Vector256<uint> rOut = ((rSrc * a) + (rDst * a)) >> 8;

            return alpha | (rOut << 16) | (gOut << 8) | bOut;
        }

        static Vector<uint> BlendBGRA(Vector<uint> bgraDst, Vector<uint> bgraSrc)
        {
            Vector<uint> gMask = Vector.GreaterThan(bgraSrc, Vector<uint>.Zero);
            bgraSrc = Vector.ConditionalSelect(gMask, bgraSrc, bgraDst);

            Vector<uint> a = Vector.Create((uint)127);
            Vector<uint> byteMask = Vector.Create((uint)0xFF);
            Vector<uint> alpha = Vector.Create((uint)byte.MaxValue << 24);

            Vector<uint> bDst = bgraDst & byteMask;
            Vector<uint> bSrc = bgraSrc & byteMask;

            Vector<uint> gDst = (bgraDst >> 8) & byteMask;
            Vector<uint> gSrc = (bgraSrc >> 8) & byteMask;

            Vector<uint> rDst = (bgraDst >> 16) & byteMask;
            Vector<uint> rSrc = (bgraSrc >> 16) & byteMask;

            Vector<uint> bOut = ((bSrc * a) + (bDst * a)) >> 8;
            Vector<uint> gOut = ((gSrc * a) + (gDst * a)) >> 8;
            Vector<uint> rOut = ((rSrc * a) + (rDst * a)) >> 8;

            return alpha | (rOut << 16) | (gOut << 8) | bOut;
        }
    }
}
