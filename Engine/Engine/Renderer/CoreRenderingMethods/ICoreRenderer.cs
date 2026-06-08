using SoftwareRendererModels;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace RenderingEngine.Engine;

internal unsafe interface ICoreRenderer
{
    static abstract void RenderFloorOrCeilingSprite(float* xMapPosMultiplierCachePtr, float* incrCachePtr, ushort* repeatedCount, uint* screenPtr, uint* texturePtr, int from, int to, int* fromYPtr, int* toYPtr, int width, float cameraPosition, int yOffset, int xOffset, int textureWidth, int textureHeightMask, int textureWidthMask, bool rotated, Vector<float> rSinV, Vector<float> rCosV, Vector<float> alignXV, Vector<float> alignYV, Vector<float> xScaleV, Vector<float> yScaleV, Vector<float> pSinV, Vector<float> pCosV, Vector<float> pxV, Vector<float> pyV);
    static abstract void RenderMultipleHorizontalLines(uint count, uint width, uint x, uint* startY, uint* endY, uint min_t, uint max_t, uint min_b, uint max_b, uint* textureYPos_u, uint* textureYIncr_u, uint* screenPtr, uint* texturePos, uint* textureBuffer);
    static abstract void RenderSpriteHorizontally(
        int spriteFromX, int spriteToX,
        uint width,
        ushort* repeatedCount,
        uint* texturePtr,
        uint* screenPtr,
        uint* portalFromClampedPtr,
        uint* portalToClampedPtr,
        uint* textureXLocationPtr,
        uint* textureYLocationPtr,
        uint* textureYIncrementPtr
        );
    static abstract void RenderMultipleWallLines(uint count, uint width, uint x, int textureHeight, uint* startY, uint* endY, uint* textureYPos_u, uint* textureYIncr_u, uint* screenPtr, uint* texturePos, uint* textureBuffer);
    static abstract void RenderMultipleWallLinesV128(uint width, uint x, int textureHeight, uint* startY, uint* endY, uint* textureYPos_u, uint* textureYIncr_u, uint* screenPtr, uint* texturePos, uint* textureBuffer);
    static abstract void RenderMultipleWallLinesV256(uint width, uint x, int textureHeight, uint* startY, uint* endY, uint* textureYPos_u, uint* textureYIncr_u, uint* screenPtr, uint* texturePos, uint* textureBuffer);
    static abstract void RenderWall(int spriteFromX, int spriteToX, uint width, int textureHeight, ushort* repeatedCount, uint* texturePtr, uint* screenPtr, uint* portalFromClampedPtr, uint* portalToClampedPtr, uint* textureXLocationPtr, uint* textureYLocationPtr, uint* textureYIncrementPtr);
    static abstract void RenderWallColumn(uint width, uint x, int textureHeight, uint startY, uint endY, uint textureYPos_u, uint textureYIncr_u, uint* screenPtr, uint* textureBuffer);
    static abstract uint RenderWallColumn2(uint width, uint x, int textureHeight, uint startY, uint endY, uint textureYPos_u, uint textureYIncr_u, uint* screenPtr, uint* textureBuffer);

    static abstract void RenderSkybox(PortalPlayerSnapshot player,
           int repeatCount,
           ushort* repeatedCount,
           float* angleCachePtr,
           uint* screenPtr,
           uint* texturePtr,
           int sectorFromX, int sectorToX,
           int* fromYPtr, int* toYPtr,
           int* ceilingStartPtr, int* floorEndPtr,
           int width,
           int textureWidth,
           int textureHeight,
           float yTextureIncr);

    public static (int min_t, int max_t, int min_b, int max_b) CalculateLaneTopBottoms(Vector<int> from, Vector<int> to)
    {
        if (Vector<int>.Count == 8)
        {
            Vector256<int> fromV = from.AsVector256();
            Vector256<int> toV = to.AsVector256();

            (int min_t, int max_t) = MathFormulas.GetMinMaxValue(fromV);
            (int min_b, int max_b) = MathFormulas.GetMinMaxValue(toV);

            return (min_t, max_t, min_b, max_b);
        }
        else if (Vector<int>.Count == 4)
        {
            Vector128<int> fromV = from.AsVector128();
            Vector128<int> toV = to.AsVector128();

            (int min_t, int max_t) = MathFormulas.GetMinMaxValue(fromV);
            (int min_b, int max_b) = MathFormulas.GetMinMaxValue(toV);

            return (min_t, max_t, min_b, max_b);
        }
        else
        {
            int min_t = int.MaxValue, max_t = int.MinValue;
            int min_b = int.MaxValue, max_b = int.MinValue;

            // compute per-lane tops/bottoms
            for (int i = 0; i < Vector<int>.Count; i++)
            {
                int top = from[i];
                min_t = MathFormulas.Min(min_t, top);
                max_t = MathFormulas.Max(max_t, top);

                int bottom = to[i];
                min_b = MathFormulas.Min(min_b, bottom);
                max_b = MathFormulas.Max(max_b, bottom);
            }

            return (min_t, max_t, min_b, max_b);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int min_t, int max_t, int min_b, int max_b) CalculateLaneTopBottoms(int x, int* from, int* to)
    {
        from += x;
        to += x;

        if (Vector<int>.Count == 8)
        {
            Vector256<int> fromV = Vector256.Load(from);
            Vector256<int> toV = Vector256.Load(to);

            (int min_t, int max_t) = MathFormulas.GetMinMaxValue(fromV);
            (int min_b, int max_b) = MathFormulas.GetMinMaxValue(toV);

            return (min_t, max_t, min_b, max_b);
        }
        else if (Vector<int>.Count == 4)
        {
            Vector128<int> fromV = Vector128.Load(from);
            Vector128<int> toV = Vector128.Load(to);

            (int min_t, int max_t) = MathFormulas.GetMinMaxValue(fromV);
            (int min_b, int max_b) = MathFormulas.GetMinMaxValue(toV);

            return (min_t, max_t, min_b, max_b);
        }
        else
        {
            int min_t = int.MaxValue, max_t = int.MinValue;
            int min_b = int.MaxValue, max_b = int.MinValue;

            // compute per-lane tops/bottoms
            for (int i = 0; i < Vector<int>.Count; i++)
            {
                int top = from[i];
                min_t = MathFormulas.Min(min_t, top);
                max_t = MathFormulas.Max(max_t, top);

                int bottom = to[i];
                min_b = MathFormulas.Min(min_b, bottom);
                max_b = MathFormulas.Max(max_b, bottom);
            }

            return (min_t, max_t, min_b, max_b);
        }
    }
}
