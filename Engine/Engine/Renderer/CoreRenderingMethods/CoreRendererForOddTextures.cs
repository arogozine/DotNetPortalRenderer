using SoftwareRendererModels;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace RenderingEngine.Engine;

/// <summary>
/// For rendering textures that are not Power of 2.
/// NOTE: Slowest approach due to modulo (%). Whenever possible use <see cref="CoreRendererForPowTextures{T}"/> or
/// <see cref="CoreRendererForUntiledTextures{T}"/>
/// </summary>
/// <typeparam name="T">How each pixel, or pixel line, is drawn</typeparam>
internal sealed unsafe class CoreRendererForOddTextures<T> : ICoreRenderer<T>
    where T : IDrawPixel
{
    private CoreRendererForOddTextures() { }

    public static void RenderSkybox(PortalPlayerSnapshot player,
        int repeatCount,
        ushort* repeatedCount,
        float* angleCachePtr,
        uint* screenPtr,
        uint* texturePtr,
        int sectorFromX,
        int sectorToX,
        int* fromYPtr,
        int* toYPtr,
        int* ceilingStartPtr,
        int* floorEndPtr,
        int width,
        int textureWidth,
        int textureHeight,
        float yTextureIncr)
    {
        if (Sse.IsSupported)
        {
            Sse.Prefetch2(texturePtr);
        }

        const float oneOverTwoPi = 1f / (2 * MathF.PI);
        float viewAngle = player.Angle;
        float textureWidth4 = textureWidth * repeatCount * oneOverTwoPi;

        Vector<float> textureWidth4V = Vector.Create(textureWidth4);
        Vector<float> yTextureIncrV = Vector.Create(yTextureIncr);
        Vector<float> viewAngleV = Vector.Create(viewAngle);

        for (int x = sectorFromX; x < sectorToX;)
        {
            ushort count = repeatedCount[x - sectorFromX];

            if (count == 0)
            {
                x++;
                continue;
            }

            if (count >= Vector<int>.Count)
            {
                Vector<int> wallStartY = Vector.Load(fromYPtr + x);
                Vector<int> wallEndY = Vector.Load(toYPtr + x);

                Vector<int> ceilingStartY = Vector.Load(ceilingStartPtr + x);
                Vector<int> floorEndY = Vector.Load(floorEndPtr + x);

                wallStartY = Vector.ClampNative(wallStartY, ceilingStartY, floorEndY);
                wallEndY = Vector.ClampNative(wallEndY, ceilingStartY, floorEndY);

                (int min_t, int max_t, int min_b, int max_b) = ICoreRenderer<T>.CalculateLaneTopBottoms(wallStartY, wallEndY);

                if (min_b > max_t)
                {
                    RenderLine(x, wallEndY, wallStartY, min_t, max_t, min_b, max_b);
                }
                else
                {
                    for (int i = 0; i < Vector<int>.Count; i++)
                    {
                        RenderColumn(wallStartY[i], wallEndY[i], x);
                    }
                }

                x += Vector<int>.Count;
                count -= (ushort)Vector<int>.Count;

                continue;
            }

            while (count-- > 0)
            {
                int wallStartY = fromYPtr[x];
                int wallEndY = toYPtr[x];

                int ceilingStartY = ceilingStartPtr[x];
                int floorEndY = floorEndPtr[x];

                wallStartY = Math.Clamp(wallStartY, ceilingStartY, floorEndY);
                wallEndY = Math.Clamp(wallEndY, ceilingStartY, floorEndY);


                RenderColumn(wallStartY, wallEndY, x);

                x++;
            }
        }

        return;

        void RenderLine(
            int x,
            Vector<int> to, Vector<int> from,
            int min_t, int max_t, int min_b, int max_b
            )
        {
            Vector<float> angleXV = MathFormulas.ClampAngle(Vector.Load(angleCachePtr + x) - viewAngleV);
            Vector<float> vScreenV = max_t * yTextureIncrV;
            Vector<int> texXV = Vector.ConvertToInt32Native(textureWidth4V * angleXV);

            for (int i = 0; i < Vector<int>.Count; i++)
            {
                ref int v = ref Unsafe.Add(ref Unsafe.As<Vector<int>, int>(ref texXV), i);
                v %= textureWidth;
            }

            // render tops where there is no shared window
            if (min_t != max_t)
            {
                RenderColumnAngleTop(min_t, max_t, from, texXV, x);
            }

            uint* fromPtr = screenPtr + max_t * width + x;

            if (Avx2.IsSupported && Vector<uint>.Count == Vector256<uint>.Count)
            {
                for (int y = max_t; y <= min_b; y++)
                {
                    Vector<int> textureIndex = texXV + textureWidth * Vector.ConvertToInt32Native(vScreenV);
                    Vector256<uint> gathered = Avx2.GatherVector256(texturePtr, textureIndex.AsVector256(), scale: sizeof(int));
                    gathered.Store(fromPtr);

                    fromPtr += width;
                    vScreenV += yTextureIncrV;
                }
            }
            else
            {
                for (int y = max_t; y <= min_b; y++)
                {
                    Vector<int> textureIndex = texXV + textureWidth * Vector.ConvertToInt32Native(vScreenV);

                    for (int i = 0; i < Vector<float>.Count; i++)
                    {
                        *(fromPtr + i) = *(texturePtr + textureIndex[i]);
                    }

                    fromPtr += width;
                    vScreenV += yTextureIncrV;
                }
            }

            // render bottoms where there is no shared window
            if (min_b != max_b)
            {
                RenderColumnAngleBottom(min_b, max_b, to, texXV, x);
            }
        }

        void RenderColumnAngleBottom(
            int min_b,
            int max_b,
            Vector<int> to,
            Vector<int> texXV,
            int xStart)
        {
            uint* screenTexPtr = screenPtr + min_b * width + xStart;

            float vScreen = min_b * yTextureIncr;

            for (int y = min_b; y < max_b; y++)
            {
                int yIndex = textureWidth * float.ConvertToIntegerNative<int>(vScreen);

                for (int i = 0; i < Vector<uint>.Count; i++)
                {
                    if (to[i] <= y)
                    {
                        continue;
                    }

                    int index = texXV[i] + yIndex;
                    screenTexPtr[i] = texturePtr[index];

                }

                screenTexPtr += width;
                vScreen += yTextureIncr;
            }
        }

        void RenderColumnAngleTop(
            int min_t,
            int max_t,
            Vector<int> from,
            Vector<int> texXV,
            int xStart)
        {
            uint* screenTexPtr = screenPtr + min_t * width + xStart;

            float vScreen = min_t * yTextureIncr;

            for (int y = min_t; y < max_t; y++)
            {
                int yIndex = textureWidth * float.ConvertToIntegerNative<int>(vScreen);

                for (int i = 0; i < Vector<uint>.Count; i++)
                {
                    if (from[i] >= y)
                    {
                        continue;
                    }

                    int index = texXV[i] + yIndex;
                    screenTexPtr[i] = texturePtr[index];
                }

                screenTexPtr += width;
                vScreen += yTextureIncr;
            }
        }

        void RenderColumn(int fromY, int toY, int x)
        {
            uint* fromPtr = screenPtr + fromY * width + x;
            uint* toPtr = screenPtr + toY * width + x;

            float angleX = *(angleCachePtr + x) - viewAngle;
            angleX = MathFormulas.ClampAngle(angleX);

            int texX = float.ConvertToIntegerNative<int>(textureWidth4 * angleX) % textureWidth;

            float vScreen = fromY * yTextureIncr;
            uint* textureColumnPtr = texturePtr + texX;

            for (; fromPtr < toPtr; vScreen += yTextureIncr, fromPtr += width)
            {
                int index = textureWidth * float.ConvertToIntegerNative<int>(vScreen);
                uint tex = *(textureColumnPtr + index);
                *fromPtr = tex;
            }
        }
    }

    public static void RenderWall(
        int spriteFromX, int spriteToX,
        uint width, int textureHeight,
        ushort* repeatedCount,
        uint* texturePtr,
        uint* screenPtr,
        uint* portalFromClampedPtr,
        uint* portalToClampedPtr,
        uint* textureXLocationPtr,
        uint* textureYLocationPtr,
        uint* textureYIncrementPtr
        )
    {
        for (int x = spriteFromX; x <= spriteToX;)
        {
            ushort count = repeatedCount[x - spriteFromX];

            if (count == 0)
            {
                x++;
                continue;
            }

            uint* clampedFromY = portalFromClampedPtr + x;
            uint* clampedToY = portalToClampedPtr + x;
            uint* textureXPos = textureXLocationPtr + x;
            uint* textureYPos = textureYLocationPtr + x;
            uint* textureYIncr = textureYIncrementPtr + x;

            // Debug.Assert(*clampedFromY < *clampedToY);

            if (Vector256.IsHardwareAccelerated && count >= Vector256<uint>.Count)
            {
                RenderMultipleWallLinesV256(
                    width,
                    (uint)x,
                    textureHeight,
                    clampedFromY,
                    clampedToY,
                    textureYPos,
                    textureYIncr,
                    screenPtr,
                    textureXPos,
                    texturePtr
                );

                x += Vector256<uint>.Count;
                continue;
            }

            if (Vector128.IsHardwareAccelerated && count >= Vector128<uint>.Count)
            {
                RenderMultipleWallLinesV128(
                    width,
                    (uint)x,
                    textureHeight,
                    clampedFromY,
                    clampedToY,
                    textureYPos,
                    textureYIncr,
                    screenPtr,
                    textureXPos,
                    texturePtr
                );

                x += Vector128<uint>.Count;
                continue;
            }

            RenderMultipleWallLines(
                count,
                width,
                (uint)x,
                textureHeight,
                clampedFromY,
                clampedToY,
                textureYPos,
                textureYIncr,
                screenPtr,
                textureXPos,
                texturePtr
            );

            x += count;
        }
    }

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

        for (int x = from; x < to;)
        {
            int count = repeatedCount[x - from];

            if (count == 0)
            {
                x++;
                continue;
            }

            // Attempt horizontal rendering
            if (count >= Vector<int>.Count)
            {
                (int min_t, int max_t, int min_b, int max_b) = ICoreRenderer<T>.CalculateLaneTopBottoms(x, fromYPtr, toYPtr);

                if (min_b > max_t + 16)
                {
                    RenderLine(x, fromYPtr + x, toYPtr + x,
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
            int* to, int* from,
            int min_t, int max_t, int min_b, int max_b
            )
        {
            Vector<float> xMapPosMultiplierCacheV = Vector.Load(xMapPosMultiplierCachePtr + x);

            // render tops where there is no shared window
            if (min_t != max_t)
            {
                RenderColumnAngleTop(min_t, max_t, from, x);
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
                RenderColumnAngleBottom(min_b, max_b, to, x);
            }
        }

        void RenderColumnAngleBottom(
            int floorFromY,
            int floorToY,
            int* to,
            int xStart)
        {
            uint* screenTexPtr = screenPtr + floorFromY * width + xStart;
            Vector<float> xMapPosMultV = Vector.Load(xMapPosMultiplierCachePtr + xStart);

            float* incr = incrCachePtr + floorFromY;

            for (int y = floorFromY; y < floorToY; y++)
            {
                for (int i = 0; i < Vector<uint>.Count; i++)
                {
                    if (to[i] > y)
                    {
                        float xMult = xMapPosMultV[i];
                        int textureIndex = GetXyFromScreenSpaceScalar(*incr, xMult);
                        T.Draw(screenTexPtr + i, texturePtr[textureIndex]);
                    }
                }

                screenTexPtr += width;
                incr++;
            }
        }

        void RenderColumnAngleTop(
            int min_t,
            int max_t,
            int* from,
            int xStart)
        {
            uint* screenTexPtr = screenPtr + min_t * width + xStart;
            Vector<float> xMapPosMultV = Vector.Load(xMapPosMultiplierCachePtr + xStart);

            float* incr = incrCachePtr + min_t;

            for (int y = min_t; y < max_t; y++)
            {
                for (int i = 0; i < Vector<uint>.Count; i++)
                {
                    if (from[i] >= y)
                    {
                        continue;
                    }

                    float xMult = xMapPosMultV[i];
                    int textureIndex = GetXyFromScreenSpaceScalar(*incr, xMult);
                    T.Draw(screenTexPtr + i, texturePtr[textureIndex]);
                }

                screenTexPtr += width;
                incr++;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetXyFromScreenSpaceScalar(float increment, float xMapPosMultiplier)
        {
            float yMapPosR = cameraPosition * increment;
            float xMapPosR = yMapPosR * xMapPosMultiplier;

            (float xMapPos, float yMapPos) = SharedHelpers.RotateVertexBack(xMapPosR, yMapPosR, pSin, pCos, px, py);

            if (rotated)
            {
                xMapPos -= alignX;
                yMapPos -= alignY;

                float xMapPosSR = MathF.FusedMultiplyAdd(xMapPos, rCos, -yMapPos * rSin);
                float yMapPosSR = MathF.FusedMultiplyAdd(xMapPos, rSin, yMapPos * rCos);

                xMapPos = xMapPosSR;
                yMapPos = yMapPosSR;
            }

            int _y1 = float.ConvertToIntegerNative<int>(yMapPos);
            int _x1 = float.ConvertToIntegerNative<int>(xMapPos);

            _y1 = (_y1 + yOffset) & textureHeightMask;
            _x1 = (_x1 + xOffset) & textureWidthMask;

            return _y1 * textureWidth + _x1;
        }
    }

    public static void RenderSpriteHorizontally(
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
    )
    {
        for (int x = spriteFromX; x <= spriteToX;)
        {
            ushort count = repeatedCount[x - spriteFromX];

            if (count == 0)
            {
                x++;
                continue;
            }

            uint* clampedFromY = portalFromClampedPtr + x;
            uint* clampedToY = portalToClampedPtr + x;
            uint* textureXPos = textureXLocationPtr + x;
            uint* textureYPos = textureYLocationPtr + x;
            uint* textureYIncr = textureYIncrementPtr + x;

            (uint min_t, uint max_t) = MathFormulas.GetMinMaxValue(clampedFromY, count);
            (uint min_b, uint max_b) = MathFormulas.GetMinMaxValue(clampedToY, count);

            ICoreRenderer<T>.RenderMultipleHorizontalLines(count, width, (uint)x, clampedFromY, clampedToY, min_t, max_t, min_b, max_b, textureYPos, textureYIncr, screenPtr, textureXPos, texturePtr);

            x += count;
        }
    }

    public static void RenderMultipleWallLinesV256(
        uint width,
        uint x,
        int textureHeight,
        uint* startY,
        uint* endY,
        uint* textureYPos_u,
        uint* textureYIncr_u,
        uint* screenPtr,
        uint* texturePos,
        uint* textureBuffer
    )
    {
        Vector256<uint> startYV = Vector256.Load(startY);
        Vector256<uint> endYV = Vector256.Load(endY);
        Vector256<uint> textureYIncr_uV = Vector256.Load(textureYIncr_u);

        (uint min_t, uint max_t) = MathFormulas.GetMinMaxValue(startYV);
        (uint min_b, uint max_b) = MathFormulas.GetMinMaxValue(endYV);

        uint textureHeightU = (uint)(textureHeight);
        Vector256<uint> textureYPos_uV = Vector256.Load(textureYPos_u);
        Vector256<uint> textureXPosV = Vector256.Load(texturePos);

        uint* screenIndexPtr = screenPtr + min_t * width + x;

        if (min_b <= max_t)
        {
            for (uint y = min_t; y < max_b; y++, screenIndexPtr += width)
            {
                Vector256<uint> yV = Vector256.Create(y);
                Vector256<uint> maskV = Vector256.LessThan(startYV, yV) & Vector256.GreaterThan(endYV, yV);

                Vector256<uint> textureYPosV = textureYPos_uV >> 16;

                for (int i = 0; i < Vector256<uint>.Count; i++)
                {
                    if (maskV[i] == 0U)
                        continue;

                    uint texelIndex = textureYPosV[i] % textureHeightU + textureXPosV[i];
                    uint pixel = *(textureBuffer + texelIndex);
                    T.Draw(screenIndexPtr + i, pixel);
                }

                textureYPos_uV = Vector256.ConditionalSelect(maskV, textureYPos_uV + textureYIncr_uV, textureYPos_uV);
            }

            return;
        }

        // render tops where there is no shared window
        if (min_t != max_t)
        {
            RenderTops();
        }

        if (min_b > max_t)
        {
            // prepare for the shared vertical window
            uint* screenIndexPtrEnd = screenPtr + (min_b * width + x);

            // go down the column set
            while (screenIndexPtr < screenIndexPtrEnd)
            {
                Vector256<uint> texelIndexV = textureYPos_uV >> 16;

                // horizontally draw the texture (keeps per-lane behavior)
                for (int i = 0; i < Vector256<uint>.Count; i++)
                {
                    uint pixel = *(textureBuffer + textureXPosV[i] + (texelIndexV[i] % textureHeightU));

                    T.Draw(screenIndexPtr + i, pixel);
                }

                textureYPos_uV += textureYIncr_uV;
                screenIndexPtr += width;
            }
        }

        // render bottoms where there is no shared window
        if (min_b == max_b)
        {
            return;
        }

        RenderBottoms();

        return;

        void RenderTops()
        {
            for (uint y = min_t; y < max_t; y++)
            {
                Vector256<uint> textureYPos = textureYPos_uV >> 16;
                Vector256<uint> mask = Vector256.LessThan(startYV, Vector256.Create(y));

                for (int i = 0; i < Vector256<uint>.Count; i++)
                {
                    if (mask[i] == 0U)
                        continue;

                    uint texelIndex = (textureYPos[i] % textureHeightU) + textureXPosV[i];

                    uint pixel = *(textureBuffer + texelIndex);
                    T.Draw((screenIndexPtr + i), pixel);
                }

                textureYPos_uV = Vector256.ConditionalSelect(mask, textureYPos_uV + textureYIncr_uV, textureYPos_uV);
                screenIndexPtr += width;
            }
        }

        void RenderBottoms()
        {
            for (uint y = min_b; y < max_b; y++)
            {
                Vector256<uint> textureYPos = textureYPos_uV >> 16;
                Vector256<uint> mask = Vector256.GreaterThan(endYV, Vector256.Create(y));

                for (int i = 0; i < Vector256<uint>.Count; i++)
                {
                    if (mask[i] == 0U)
                        continue;

                    uint texelIndex = (textureYPos[i] % textureHeightU) + textureXPosV[i];

                    uint pixel = *(textureBuffer + texelIndex);
                    T.Draw((screenIndexPtr + i), pixel);
                }

                textureYPos_uV = Vector256.ConditionalSelect(mask, textureYPos_uV + textureYIncr_uV, textureYPos_uV);
                screenIndexPtr += width;
            }
        }
    }

    public static void RenderMultipleWallLinesV128(
        uint width,
        uint x,
        int textureHeight,
        uint* startY,
        uint* endY,
        uint* textureYPos_u,
        uint* textureYIncr_u,
        uint* screenPtr,
        uint* texturePos,
        uint* textureBuffer
        )
    {
        var startYV = Vector128.Load(startY);
        var endYV = Vector128.Load(endY);
        var textureXIncr_uV = Vector128.Load(textureYIncr_u);

        (uint min_t, uint max_t) = MathFormulas.GetMinMaxValue(startYV);
        (uint min_b, uint max_b) = MathFormulas.GetMinMaxValue(endYV);

        uint textureHeightU = (uint)(textureHeight);
        Vector128<uint> textureYPos_uV = Vector128.Load(textureYPos_u);
        Vector128<uint> textureXPosV = Vector128.Load(texturePos);

        if (min_b <= max_t)
        {
            uint* screenIndexPtr2 = screenPtr + min_t * width + x;

            for (uint y = min_t; y < max_b; y++, screenIndexPtr2 += width)
            {
                Vector128<uint> yV = Vector128.Create(y);
                Vector128<uint> mask = Vector128.LessThan(startYV, yV) & Vector128.GreaterThan(endYV, yV);

                Vector128<uint> textureYPos = textureYPos_uV >> 16;

                for (int i = 0; i < Vector128<uint>.Count; i++)
                {
                    if (mask[i] == 0U)
                        continue;

                    uint texelIndex = textureYPos[i] % textureHeightU + textureXPosV[i];
                    uint pixel = *(textureBuffer + texelIndex);
                    T.Draw(screenIndexPtr2 + i, pixel);
                }

                textureYPos_uV = Vector128.ConditionalSelect(mask, textureYPos_uV + textureXIncr_uV, textureYPos_uV);
            }

            return;
        }

        uint* screenIndexPtr = screenPtr + min_t * width + x;

        // render tops where there is no shared window
        if (min_t != max_t)
        {
            RenderTops();
        }

        if (min_b > max_t)
        {
            // prepare for the shared vertical window
            uint* screenIndexPtrEnd = screenPtr + (min_b * width + x);

            // go down the column set
            while (screenIndexPtr < screenIndexPtrEnd)
            {
                Vector128<uint> texelIndexV = textureYPos_uV >> 16;

                // horizontally draw the texture (keeps per-lane behavior)
                for (int i = 0; i < Vector128<uint>.Count; i++)
                {
                    uint pixel = *(textureBuffer + textureXPosV[i] + (texelIndexV[i] % textureHeightU));

                    T.Draw(screenIndexPtr + i, pixel);
                }

                textureYPos_uV += textureXIncr_uV;
                screenIndexPtr += width;
            }
        }

        // render bottoms where there is no shared window
        if (min_b == max_b)
        {
            return;
        }

        RenderBottoms();

        void RenderTops()
        {
            for (uint y = min_t; y < max_t; y++)
            {
                Vector128<uint> textureYPos = textureYPos_uV >> 16;
                Vector128<uint> mask = Vector128.LessThan(startYV, Vector128.Create(y));

                for (int i = 0; i < Vector128<uint>.Count; i++)
                {
                    if (mask[i] == 0U)
                        continue;

                    uint texelIndex = (textureYPos[i] % textureHeightU) + textureXPosV[i];

                    uint pixel = *(textureBuffer + texelIndex);
                    T.Draw((screenIndexPtr + i), pixel);
                }

                textureYPos_uV = Vector128.ConditionalSelect(mask, textureYPos_uV + textureXIncr_uV, textureYPos_uV);
                screenIndexPtr += width;
            }
        }

        void RenderBottoms()
        {
            for (uint y = min_b; y < max_b; y++)
            {
                Vector128<uint> textureYPos = textureYPos_uV >> 16;
                Vector128<uint> mask = Vector128.GreaterThan(endYV, Vector128.Create(y));

                for (int i = 0; i < Vector128<uint>.Count; i++)
                {
                    if (mask[i] == 0U)
                        continue;

                    uint texelIndex = (textureYPos[i] % textureHeightU) + textureXPosV[i];

                    uint pixel = *(textureBuffer + texelIndex);
                    T.Draw((screenIndexPtr + i), pixel);
                }

                textureYPos_uV = Vector128.ConditionalSelect(mask, textureYPos_uV + textureXIncr_uV, textureYPos_uV);
                screenIndexPtr += width;
            }
        }
    }

    public static void RenderMultipleWallLines(
        uint count,
        uint width,
        uint x,
        int textureHeight,
        uint* startY,
        uint* endY,
        uint* textureYPos_u,
        uint* textureYIncr_u,
        uint* screenPtr,
        uint* texturePos,
        uint* textureBuffer
        )
    {
        // each line can start and end at different y positions
        // so we determine the window where all lines can be rendered at once
        uint min_t = int.MaxValue, max_t = 0;
        uint min_b = int.MaxValue, max_b = 0;

        for (int i = 0; i < count; i++)
        {
            uint top = *(startY + i);
            min_t = Math.Min(min_t, top);
            max_t = Math.Max(max_t, top);

            uint bottom = *(endY + i);
            min_b = Math.Min(min_b, bottom);
            max_b = Math.Max(max_b, bottom);
        }

        if (min_b <= max_t)
        {
            for (uint i = 0; i < count; i++, x++)
            {
                uint textureYPos = *(textureYPos_u + i);
                uint incr = *(textureYIncr_u + i);
                uint top = *(startY + i);
                uint bottom = *(endY + i);

                RenderWallColumn(width, x, textureHeight, top, bottom, textureYPos, incr, screenPtr,
                    textureBuffer + *(texturePos + i));
            }

            return;
        }

        // render tops of each line where there is no shared window
        if (min_t < max_t)
        {
            for (uint i = 0; i < count; i++)
            {
                uint top = *(startY + i);

                if (top >= max_t)
                {
                    continue;
                }

                uint* textureYPos = textureYPos_u + i;
                uint incr = *(textureYIncr_u + i);

                uint xi = x + i;

                *textureYPos = RenderWallColumn2(width, xi, textureHeight, top, max_t, *textureYPos, incr, screenPtr,
                    textureBuffer + *(texturePos + i));
            }
        }

        // shared window
        if (min_b > max_t)
        {
            uint* screenIndexPtr = screenPtr + max_t * width + x;
            uint* screenIndexPtrEnd = screenPtr + min_b * width + x;

            uint textureMask = (uint)textureHeight;

            // go down the column set
            while (screenIndexPtr < screenIndexPtrEnd)
            {
                // horizontally draw the texture
                for (int i = 0; i < count; i++)
                {
                    uint* textureYPos = textureYPos_u + i;
                    uint texelIndex = (*textureYPos >> 16) % textureMask;
                    texelIndex += *(texturePos + i);

                    uint pixel = *(textureBuffer + texelIndex);

                    T.Draw(screenIndexPtr, pixel);

                    *textureYPos += *(textureYIncr_u + i);
                    screenIndexPtr++;
                }

                screenIndexPtr += width - count;
            }
        }

        // render bottoms of each line where there is no shared window
        if (min_b >= max_b)
        {
            return;
        }

        for (uint i = 0; i < count; i++, x++)
        {
            uint bottom = *(endY + i);

            if (bottom <= min_b)
            {
                continue;
            }

            uint textureYPos = *(textureYPos_u + i);
            uint incr = *(textureYIncr_u + i);

            RenderWallColumn(width, x, textureHeight, min_b, bottom, textureYPos, incr, screenPtr,
                textureBuffer + *(texturePos + i));
        }
    }

    public static uint RenderWallColumn2(
        uint width,
        uint x,
        int textureHeight,
        uint startY,
        uint endY,
        uint textureYPos_u,
        uint textureYIncr_u,
        uint* screenPtr,
        uint* textureBuffer
        )
    {
        Debug.Assert(endY >= startY);
        uint* screenIndexPtr = screenPtr + startY * width + x;
        uint* screenIndexPtrEnd = screenPtr + endY * width + x;

        uint textureHeightMask = (uint)textureHeight;

        while (screenIndexPtr != screenIndexPtrEnd)
        {
            uint texelIndex = (textureYPos_u >> 16) % textureHeightMask;
            uint pixel = *(textureBuffer + texelIndex);

            T.Draw(screenIndexPtr, pixel);

            screenIndexPtr += width;
            textureYPos_u += textureYIncr_u;
        }

        return textureYPos_u;
    }

    public static void RenderWallColumn(
        uint width,
        uint x,
        int textureHeight,
        uint startY,
        uint endY,
        uint textureYPos_u,
        uint textureYIncr_u,
        uint* screenPtr,
        uint* textureBuffer
        )
    {
        Debug.Assert(endY >= startY);
        uint* screenIndexPtr = screenPtr + startY * width + x;
        uint* screenIndexPtrEnd = screenPtr + endY * width + x;

        uint textureHeightMask = (uint)textureHeight;

        while (screenIndexPtr < screenIndexPtrEnd)
        {
            uint texelIndex = (textureYPos_u >> 16) % textureHeightMask;
            uint pixel = *(textureBuffer + texelIndex);

            T.Draw(screenIndexPtr, pixel);

            screenIndexPtr += width;
            textureYPos_u += textureYIncr_u;
        }
    }
}
