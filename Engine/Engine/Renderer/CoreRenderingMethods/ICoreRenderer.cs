using SoftwareRendererModels;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace RenderingEngine.Engine;

internal unsafe interface ICoreRenderer<T>
    where T : IDrawPixel
{
    public static void RenderFloorOrCeilingSprite(
        float* xMapPosMultiplierCachePtr,
        float* incrCachePtr,
        ushort* repeatedCount,
        uint* screenPtr,
        uint* texturePtr,
        int from, int to,
        int* fromYPtr,
        int* toYPtr,
        int width,
        float cameraPosition,
        int yOffset,
        int xOffset,
        int textureWidth,
        int textureHeightMask,
        int textureWidthMask,
        bool rotated,
        Vector<float> rSinV,
        Vector<float> rCosV,
        Vector<float> alignXV,
        Vector<float> alignYV,
        Vector<float> xScaleV,
        Vector<float> yScaleV,
        // Player Position
        Vector<float> pSinV,
        Vector<float> pCosV,
        Vector<float> pxV,
        Vector<float> pyV
    )
    {
        // Texture
        Vector<int> textureWidthV = Vector.Create(textureWidth);
        Vector<int> textureHeightMaskV = Vector.Create(textureHeightMask);
        Vector<int> textureWidthMaskV = Vector.Create(textureWidthMask);
        Vector<int> xOffSetV = Vector.Create(xOffset);
        Vector<int> yOffSetV = Vector.Create(yOffset);
        // Player height compared to ceiling/floor
        Vector<float> cameraPositionV = Vector.Create(cameraPosition);

        // scalar
        float alignX = alignXV[0];
        float alignY = alignYV[0];
        float rSin = rSinV[0];
        float rCos = rCosV[0];
        float pSin = pSinV[0];
        float pCos = pCosV[0];
        float px = pxV[0];
        float py = pyV[0];


        for (int x = from; x <= to;)
        {
            ushort count = repeatedCount[x - from];

            if (count == 0)
            {
                x++;
                continue;
            }

            // Attempt horizontal rendering
            if (count >= Vector<int>.Count)
            {
                Vector<int> fromV = Vector.Load(fromYPtr + x);
                Vector<int> toV = Vector.Load(toYPtr + x);

                (int min_t, int max_t, int min_b, int max_b) = CalculateLaneTopBottoms(fromV, toV);

                if (min_b > max_t + 16)
                {
                    RenderLine(x, toV, fromV,
                        min_t, max_t, min_b, max_b);

                    x += Vector<int>.Count;
                    continue;
                }
            }

            while (count-- > 0)
            {
                int clampedFromY = fromYPtr[x];
                int clampedToY = toYPtr[x];
                float xMapPosMultiplier = *(xMapPosMultiplierCachePtr + x);

                RenderColumn(clampedFromY, clampedToY, x, xMapPosMultiplier);
                x++;
            }
        }

        return;

        void RenderLine(
            int x,
            Vector<int> toV, Vector<int> fromV,
            int min_t, int max_t, int min_b, int max_b
            )
        {
            Vector<float> xMapPosMultiplierCacheV = Vector.Load(xMapPosMultiplierCachePtr + x);

            // render tops where there is no shared window
            if (min_t != max_t)
            {
                RenderColumnAngleTop(min_t, max_t, fromV, x);
            }

            for (int y = max_t, screenIndex = y * width + x; y <= min_b; y++, screenIndex += width)
            {
                Vector<float> incrementVector = Vector.Create(*(incrCachePtr + y));
                Vector<int> textureIndex = GetXyFromScreenSpace(incrementVector, xMapPosMultiplierCacheV);

                uint* screenTexPtr = screenPtr + screenIndex;

                if (Avx2.IsSupported && Vector<int>.Count == Vector256<int>.Count)
                {
                    Vector256<uint> gathered = Avx2.GatherVector256(texturePtr, textureIndex.AsVector256(), scale: sizeof(int));
                    T.DrawLine(screenTexPtr, gathered);
                }
                else
                {
                    for (int i = 0; i < Vector<int>.Count; i++)
                    {
                        T.Draw(screenTexPtr + i, texturePtr[textureIndex[i]]);
                    }
                }
            }

            // render bottoms where there is no shared window
            if (min_b != max_b)
            {
                RenderColumnAngleBottom(min_b, max_b, toV, x);
            }
        }

        void RenderColumnAngleBottom(
            int floorFromY,
            int floorToY,
            Vector<int> toV,
            int xStart)
        {
            uint* screenTexPtr = screenPtr + floorFromY * width + xStart;
            Vector<float> xMapPosMultV = Vector.Load(xMapPosMultiplierCachePtr + xStart);

            Vector<float> xMapPosMultiplierCacheV = Vector.Load(xMapPosMultiplierCachePtr + xStart);

            for (int y = floorFromY; y < floorToY; y++)
            {
                Vector<uint> maskV = Vector.GreaterThan(toV.As<int, uint>(), Vector.Create((uint)y));
                Vector<float> incrementVector = Vector.Create(*(incrCachePtr + y));
                Vector<int> textureIndexV = GetXyFromScreenSpace(incrementVector, xMapPosMultiplierCacheV);

                if (Avx2.IsSupported && Vector<int>.Count == Vector256<int>.Count)
                {
                    Vector256<uint> gathered = Avx2.GatherVector256(texturePtr, textureIndexV.AsVector256(), scale: sizeof(int));
                    T.DrawLine(screenTexPtr, gathered, maskV.AsVector256());
                }
                else
                {
                    for (int i = 0; i < Vector<uint>.Count; i++)
                    {
                        if (maskV[i] != 0U)
                        {
                            float xMult = xMapPosMultV[i];
                            int textureIndex = textureIndexV[i];
                            T.Draw(screenTexPtr + i, texturePtr[textureIndex]);
                        }
                    }
                }

                screenTexPtr += width;
            }
        }

        void RenderColumnAngleTop(
            int min_t,
            int max_t,
            Vector<int> fromV,
            int xStart)
        {
            uint* screenTexPtr = screenPtr + min_t * width + xStart;
            Vector<float> xMapPosMultV = Vector.Load(xMapPosMultiplierCachePtr + xStart);
            Vector<float> xMapPosMultiplierCacheV = Vector.Load(xMapPosMultiplierCachePtr + xStart);

            for (int y = min_t; y < max_t; y++)
            {
                Vector<uint> maskV = Vector.GreaterThan(Vector.Create((uint)y), fromV.As<int, uint>());
                Vector<float> incrementVector = Vector.Create(*(incrCachePtr + y));
                Vector<int> textureIndexV = GetXyFromScreenSpace(incrementVector, xMapPosMultiplierCacheV);

                if (Avx2.IsSupported && Vector<int>.Count == Vector256<int>.Count)
                {
                    Vector256<uint> gathered = Avx2.GatherVector256(texturePtr, textureIndexV.AsVector256(), scale: sizeof(int));
                    T.DrawLine(screenTexPtr, gathered, maskV.AsVector256());
                }
                else
                {
                    for (int i = 0; i < Vector<uint>.Count; i++)
                    {
                        if (maskV[i] != 0U)
                        {
                            float xMult = xMapPosMultV[i];
                            int textureIndex = textureIndexV[i];
                            T.Draw(screenTexPtr + i, texturePtr[textureIndex]);
                        }
                    }
                }
            }
        }

        void RenderColumn(int floorFromY, int floorToY, int x, float xMapPosMultiplier)
        {
            int rem = (floorToY - floorFromY) & (Vector<int>.Count - 1);
            floorToY -= rem;

            Vector<float> incrementVector = Vector.Load(incrCachePtr + floorFromY);
            Vector<float> xMapPosMultiplierV = Vector.Create(xMapPosMultiplier);
            Vector<int> textureIndex;

            uint* screenTex = screenPtr + floorFromY * width + x;
            uint* toScalePtr = screenPtr + floorToY * width + x;

            while (screenTex != toScalePtr)
            {
                textureIndex = GetXyFromScreenSpace(incrementVector, xMapPosMultiplierV);

                if (Avx2.IsSupported && Vector<int>.Count == Vector256<int>.Count)
                {
                    Vector256<uint> gathered = Avx2.GatherVector256(
                        texturePtr,
                        textureIndex.AsVector256(),
                        scale: sizeof(uint)
                    );

                    for (int i = 0; i < Vector256<int>.Count; i++, screenTex += width)
                    {
                        uint tex = gathered[i];
                        T.Draw(screenTex, tex);
                    }
                }
                else
                {
                    for (int i = 0; i < Vector<int>.Count; i++, screenTex += width)
                    {
                        uint tex = *(texturePtr + textureIndex[i]);

                        T.Draw(screenTex, tex);
                    }
                }

                floorFromY += Vector<float>.Count;
                incrementVector = Vector.Load(incrCachePtr + floorFromY);
            }

            if (rem <= 0)
            {
                return;
            }

            textureIndex = GetXyFromScreenSpace(incrementVector, xMapPosMultiplierV);

            for (int i = 0; i < rem; i++, screenTex += width)
            {
                uint tex = *(texturePtr + textureIndex[i]);

                T.Draw(screenTex, tex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Vector<int> GetXyFromScreenSpace(
                Vector<float> incrementVector,
                Vector<float> xMapPosMultiplierV
            )
        {
            Vector<float> yMapPosR = cameraPositionV * incrementVector;
            Vector<float> xMapPosR = yMapPosR * xMapPosMultiplierV;

            (Vector<float> xMapPos, Vector<float> yMapPos) = SharedHelpers.RotateVertexBack(xMapPosR, yMapPosR, pSinV, pCosV, pxV, pyV);

            if (rotated)
            {
                xMapPos -= alignXV;
                yMapPos -= alignYV;

                Vector<float> xMapPosSR = Vector.FusedMultiplyAdd(xMapPos, rCosV, -yMapPos * rSinV);
                Vector<float> yMapPosSR = Vector.FusedMultiplyAdd(xMapPos, rSinV, yMapPos * rCosV);

                xMapPos = xMapPosSR;
                yMapPos = yMapPosSR;
            }

            Vector<int> _y1 = Vector.ConvertToInt32Native(yMapPos * yScaleV);
            Vector<int> _x1 = Vector.ConvertToInt32Native(xMapPos * xScaleV);

            _y1 = (_y1 + yOffSetV) & textureHeightMaskV;
            _x1 = (_x1 + xOffSetV) & textureWidthMaskV;

            return _y1 * textureWidthV + _x1;
        }
    }

    public static void RenderMultipleHorizontalLines(
        uint count,
        uint width,
        uint x,
        uint* startY,
        uint* endY,
        uint min_t, uint max_t,
        uint min_b, uint max_b,
        uint* textureYPos_u,
        uint* textureYIncr_u,
        uint* screenPtr,
        uint* texturePos,
        uint* textureBuffer
        )
    {
        RenderTops();
        RenderHorizontal();
        RenderBottoms();

        return;

        void RenderHorizontal()
        {
            uint* screenIndexPtr = screenPtr + max_t * width + x;
            uint* screenIndexPtrEnd = screenPtr + min_b * width + x;

            while (screenIndexPtr < screenIndexPtrEnd)
            {
                int i = 0;
                uint rem = count & ((uint)Vector<uint>.Count - 1U);
                count -= rem;

                for (; i < count; i += Vector<uint>.Count)
                {
                    uint* textureYPos = textureYPos_u + i;

                    Vector<uint> textureYPosV = Vector.Load(textureYPos);
                    Vector<uint> texturePosV = Vector.Load(texturePos + i);
                    Vector<uint> texelIndexV = (textureYPosV >> 16) + texturePosV;

                    if (Avx2.IsSupported && Vector<uint>.Count == Vector256<uint>.Count)
                    {
                        Vector256<uint> gathered = Avx2.GatherVector256(
                            textureBuffer,
                            texelIndexV.AsVector256().AsInt32(),
                            scale: sizeof(uint)
                        );

                        T.DrawLine(screenIndexPtr, gathered);
                    }
                    else
                    {
                        for (int j = 0; j < Vector<uint>.Count; j++)
                        {
                            uint pixel = *(textureBuffer + texelIndexV[j]);
                            T.Draw(screenIndexPtr + j, pixel);
                        }
                    }

                    textureYPosV += Vector.Load(textureYIncr_u + i);
                    textureYPosV.Store(textureYPos);
                    screenIndexPtr += Vector<uint>.Count;
                }

                count += rem;

                for (; i < count; i++)
                {
                    uint* textureYPos = textureYPos_u + i;
                    uint texelIndex = (*textureYPos >> 16);
                    texelIndex += *(texturePos + i);

                    uint pixel = *(textureBuffer + texelIndex);

                    T.Draw(screenIndexPtr, pixel);

                    *textureYPos += *(textureYIncr_u + i);
                    screenIndexPtr++;
                }

                screenIndexPtr += width - count;
            }
        }

        void RenderTops()
        {
            uint* screenIndexPtr = screenPtr + min_t * width + x;

            for (uint y = min_t; y < max_t; y++)
            {
                for (int i = 0; i < count; i++)
                {
                    uint start = *(startY + i);

                    if (y <= start)
                        continue;

                    uint* textureXPos = textureYPos_u + i;
                    uint texelIndex = (*textureXPos >> 16);
                    texelIndex += *(texturePos + i);

                    uint pixel = *(textureBuffer + texelIndex);

                    T.Draw((screenIndexPtr + i), pixel);

                    *textureXPos += *(textureYIncr_u + i);
                }

                screenIndexPtr += width;
            }
        }

        void RenderBottoms()
        {
            uint* screenIndexPtr = screenPtr + min_b * width + x;

            for (uint y = min_b; y < max_b; y++)
            {
                for (int i = 0; i < count; i++)
                {
                    uint end = *(endY + i);

                    if (end <= y)
                        continue;

                    uint* textureXPos = textureYPos_u + i;
                    uint texelIndex = (*textureXPos >> 16);
                    texelIndex += *(texturePos + i);

                    uint pixel = *(textureBuffer + texelIndex);

                    T.Draw((screenIndexPtr + i), pixel);

                    *textureXPos += *(textureYIncr_u + i);
                }

                screenIndexPtr += width;
            }
        }
    }
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

    static abstract void RenderWall(int spriteFromX, int spriteToX, uint width, int textureHeight, ushort* repeatedCount, uint* texturePtr, uint* screenPtr, uint* portalFromClampedPtr, uint* portalToClampedPtr, uint* textureXLocationPtr, uint* textureYLocationPtr, uint* textureYIncrementPtr);

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
        if (Vector<int>.Count == Vector256<int>.Count)
        {
            Vector256<int> fromV = from.AsVector256();
            Vector256<int> toV = to.AsVector256();

            (int min_t, int max_t) = MathFormulas.GetMinMaxValue(fromV);
            (int min_b, int max_b) = MathFormulas.GetMinMaxValue(toV);

            return (min_t, max_t, min_b, max_b);
        }
        else if (Vector<int>.Count == Vector128<int>.Count)
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
}
