using SoftwareRendererModels;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Tooling;

namespace RenderingEngine.Engine;

/// <summary>
/// For rendering textures that are Power of 2.
/// </summary>
/// <typeparam name="T">How each pixel, or pixel line, is drawn</typeparam>
internal static unsafe class CoreRendererForPowTextures<T>
    where T : IDrawPixel
{
    public static void RenderSkybox(PortalPlayerSnapshot player,
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
        Vector<int> widthMask = Vector.Create(textureWidth - 1);

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
            Vector<int> texXV = Vector.ConvertToInt32Native(textureWidth4V * angleXV) & widthMask;

            // render tops where there is no shared window
            if (min_t != max_t)
            {
                RenderColumnAngleTop(min_t, max_t, from, texXV, x);
            }

            uint* fromPtr = screenPtr + max_t * width + x;

            for (int y = max_t; y <= min_b; y++)
            {
                Vector<int> textureIndex = texXV + textureWidth * Vector.ConvertToInt32Native(vScreenV);
                Vector<uint> gathered = Vector.Gather(texturePtr, textureIndex);
                gathered.Store(fromPtr);

                fromPtr += width;
                vScreenV += yTextureIncrV;
            }

            // render bottoms where there is no shared window
            if (min_b != max_b)
            {
                RenderColumnAngleBottom(min_b, max_b, to, texXV, x);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                Vector<int> yIndexV = Vector.Create(textureWidth * float.ConvertToIntegerNative<int>(vScreen));
                Vector<int> indexV = texXV + yIndexV;
                Vector<int> maskV = Vector.GreaterThan(to, Vector.Create(y));

                Vector<uint> gathered = Vector.GatherMask(texturePtr, indexV, maskV.As<int, uint>());
                T.DrawLine(screenTexPtr, gathered, maskV.As<int, uint>());

                screenTexPtr += width;
                vScreen += yTextureIncr;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RenderColumnAngleTop(
            int min_t,
            int max_t,
            Vector<int> from,
            Vector<int> texXV,
            int xStart)
        {
            uint* screenTexPtr = screenPtr + min_t * width + xStart;

            float vScreen = min_t * yTextureIncr;

            Vector256<int> fromV256 = from.AsVector256();
            Vector256<int> texXV256 = texXV.AsVector256();

            for (int y = min_t; y < max_t; y++)
            {
                Vector256<int> yIndexV = Vector256.Create(textureWidth * float.ConvertToIntegerNative<int>(vScreen));
                Vector256<int> indexV = texXV256 + yIndexV;
                Vector256<uint> maskV = Vector256.LessThan(fromV256, Vector256.Create(y)).AsUInt32();

                Vector256<uint> gathered = Vector256.GatherMask(texturePtr, indexV, maskV.As<uint, int>());

                T.DrawLine(screenTexPtr, gathered, maskV);

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

            int texX = float.ConvertToIntegerNative<int>(textureWidth4 * angleX) & (textureWidth - 1);

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
        if (Sse.IsSupported)
        {
            Sse.Prefetch2(texturePtr);
        }

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

            Debug.Assert(*clampedFromY < *clampedToY);

            if (Vector256.IsHardwareAccelerated && count >= Vector256<uint>.Count)
            {
                if (0 == (x & (Vector256<uint>.Count - 1)))
                {
                    RenderMultipleWallLinesV256<AlignedMemory>(
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
                }
                else
                {
                    RenderMultipleWallLinesV256<UnalignedMemory>(
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
                }

                x += Vector256<uint>.Count;
                continue;
            }

            if (Vector128.IsHardwareAccelerated && count >= Vector128<uint>.Count)
            {
                if (0 == (x & (Vector128<uint>.Count - 1)))
                {
                    RenderMultipleWallLinesV128<AlignedMemory>(
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
                }
                else
                {
                    RenderMultipleWallLinesV128<UnalignedMemory>(
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
                }

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
        if (Sse.IsSupported)
        {
            Sse.Prefetch2(texturePtr);
        }

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

            bool aligned = 0 == (x & (Vector<uint>.Count - 1));

            if (aligned)
            {
                (uint min_t, uint max_t) = MathFormulas.GetMinMaxValue<AlignedMemory>(clampedFromY, count);
                (uint min_b, uint max_b) = MathFormulas.GetMinMaxValue<AlignedMemory>(clampedToY, count);

                ICoreRenderer<T>.RenderMultipleHorizontalLines<AlignedMemory>(count, width, (uint)x, clampedFromY, clampedToY, min_t, max_t, min_b, max_b, textureYPos, textureYIncr, screenPtr, textureXPos, texturePtr);
            }
            else
            {
                (uint min_t, uint max_t) = MathFormulas.GetMinMaxValue<UnalignedMemory>(clampedFromY, count);
                (uint min_b, uint max_b) = MathFormulas.GetMinMaxValue<UnalignedMemory>(clampedToY, count);

                ICoreRenderer<T>.RenderMultipleHorizontalLines<UnalignedMemory>(count, width, (uint)x, clampedFromY, clampedToY, min_t, max_t, min_b, max_b, textureYPos, textureYIncr, screenPtr, textureXPos, texturePtr);
            }

            x += count;
        }
    }

    public static void RenderMultipleWallLinesV256<I>(
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
        where I : IMemoryAlignment
    {
        Vector256<uint> startYV = I.Load256(startY);
        Vector256<uint> endYV = I.Load256(endY);
        Vector256<uint> textureYIncr_uV = I.Load256(textureYIncr_u);

        (uint min_t, uint max_t) = MathFormulas.GetMinMaxValue(startYV);
        (uint min_b, uint max_b) = MathFormulas.GetMinMaxValue(endYV);

        uint textureHeightMask = (uint)(textureHeight - 1);
        Vector256<uint> textureMaskV = Vector256.Create(textureHeightMask);

        Vector256<uint> textureXPosV = I.Load256(texturePos);
        Vector256<uint> textureYPos_uV = I.Load256(textureYPos_u);

        if (Sse.IsSupported)
        {
            Debug.Assert(textureHeight > (textureYPos_uV[0] >> 16));

            uint minTextureIndex = textureXPosV[0];
            Sse.Prefetch1(textureBuffer + minTextureIndex);
            uint maxTextureIndex = minTextureIndex + (textureYPos_uV[0] >> 16);
            Sse.Prefetch0(textureBuffer + maxTextureIndex);
        }

        uint* screenIndexPtr = screenPtr + min_t * width + x;

        if (min_b <= max_t)
        {
            for (uint y = min_t; y < max_b; y++, screenIndexPtr += width)
            {
                Vector256<uint> yV = Vector256.Create(y);
                Vector256<uint> mask = Vector256.LessThan(startYV, yV) & Vector256.GreaterThan(endYV, yV);

                Vector256<uint> texelIndexV = textureXPosV + ((textureYPos_uV >> 16) & textureMaskV);
                Vector256<uint> gathered = Vector256.GatherMask(textureBuffer, texelIndexV.AsInt32(), mask.AsInt32());
                T.DrawLine(screenIndexPtr, gathered, mask);

                textureYPos_uV += mask & textureYIncr_uV;
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

            while (screenIndexPtr < screenIndexPtrEnd)
            {
                Vector256<uint> texelIndexV = (textureYPos_uV >> 16) & textureMaskV;
                texelIndexV += textureXPosV;

                Vector256<uint> gathered = Vector256.Gather(textureBuffer, texelIndexV.AsInt32());

                T.DrawLine(screenIndexPtr, gathered);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RenderTops()
        {
            for (uint y = min_t; y < max_t; y++)
            {
                Vector256<uint> texelIndexV = (textureYPos_uV >> 16) & textureMaskV;
                texelIndexV += textureXPosV;

                Vector256<uint> mask = Vector256.LessThan(startYV, Vector256.Create(y));
                Vector256<uint> gathered = Vector256.GatherMask(textureBuffer, texelIndexV.AsInt32(), mask.As<uint, int>());
                T.DrawLine(screenIndexPtr, gathered, mask);

                textureYPos_uV += mask & textureYIncr_uV;
                screenIndexPtr += width;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RenderBottoms()
        {
            for (uint y = min_b; y < max_b; y++)
            {
                Vector256<uint> texelIndexV = (textureYPos_uV >> 16) & textureMaskV;
                texelIndexV += textureXPosV;

                Vector256<uint> mask = Vector256.GreaterThan(endYV, Vector256.Create(y));
                Vector256<uint> gathered = Vector256.GatherMask(textureBuffer, texelIndexV.AsInt32(), mask.As<uint, int>());
                T.DrawLine(screenIndexPtr, gathered, mask);

                textureYPos_uV += mask & textureYIncr_uV;
                screenIndexPtr += width;
            }
        }
    }

    public static void RenderMultipleWallLinesV128<I>(
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
        where I : IMemoryAlignment
    {
        Vector128<uint> startYV = I.Load128(startY);
        Vector128<uint> endYV = I.Load128(endY);
        Vector128<uint> textureYIncr_uV = I.Load128(textureYIncr_u);

        (uint min_t, uint max_t) = MathFormulas.GetMinMaxValue(startYV);
        (uint min_b, uint max_b) = MathFormulas.GetMinMaxValue(endYV);

        uint* screenIndexPtr = screenPtr + min_t * width + x;

        uint textureHeightMask = (uint)(textureHeight - 1);
        Vector128<uint> textureMaskV = Vector128.Create(textureHeightMask);
        Vector128<uint> textureYPos_uV = I.Load128(textureYPos_u);
        Vector128<uint> textureXPosV = I.Load128(texturePos);

        if (Sse.IsSupported)
        {
            Debug.Assert(textureHeight > (textureYPos_uV[0] >> 16));

            uint minTextureIndex = textureXPosV[0];
            Sse.Prefetch1(textureBuffer + minTextureIndex);
            uint maxTextureIndex = minTextureIndex + (textureYPos_uV[0] >> 16);
            Sse.Prefetch0(textureBuffer + maxTextureIndex);
        }

        if (min_b <= max_t)
        {
            for (uint y = min_t; y < max_b; y++, screenIndexPtr += width)
            {
                Vector128<uint> yV = Vector128.Create(y);
                Vector128<uint> mask = Vector128.LessThan(startYV, yV) & Vector128.GreaterThan(endYV, yV);

                Vector128<uint> texelIndexV = textureXPosV + ((textureYPos_uV >> 16) & textureMaskV);

                Vector128<uint> gathered = Vector128.GatherMask(textureBuffer, texelIndexV.AsInt32(), mask.As<uint, int>());
                T.DrawLine(screenIndexPtr, gathered, mask);

                textureYPos_uV += mask & textureYIncr_uV;
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

            while (screenIndexPtr < screenIndexPtrEnd)
            {
                Vector128<uint> texelIndexV = (textureYPos_uV >> 16) & textureMaskV;
                texelIndexV += textureXPosV;

                Vector128<uint> gathered = Vector128.Gather(textureBuffer, texelIndexV.AsInt32());
                T.DrawLine(screenIndexPtr, gathered);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RenderTops()
        {
            for (uint y = min_t; y < max_t; y++)
            {
                Vector128<uint> texelIndexV = (textureYPos_uV >> 16) & textureMaskV;
                texelIndexV += textureXPosV;

                Vector128<uint> mask = Vector128.LessThan(startYV, Vector128.Create(y));
                Vector128<uint> gathered = Vector128.GatherMask(textureBuffer, texelIndexV.AsInt32(), mask.AsInt32());
                T.DrawLine(screenIndexPtr, gathered, mask);

                textureYPos_uV += mask & textureYIncr_uV;
                screenIndexPtr += width;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RenderBottoms()
        {
            for (uint y = min_b; y < max_b; y++)
            {
                Vector128<uint> texelIndexV = (textureYPos_uV >> 16) & textureMaskV;
                texelIndexV += textureXPosV;

                Vector128<uint> mask = Vector128.GreaterThan(endYV, Vector128.Create(y));
                Vector128<uint> gathered = Vector128.GatherMask(textureBuffer, texelIndexV.AsInt32(), mask.AsInt32());
                T.DrawLine(screenIndexPtr, gathered, mask);

                textureYPos_uV += mask & textureYIncr_uV;
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

            uint textureMask = (uint)(textureHeight - 1);

            // go down the column set
            while (screenIndexPtr < screenIndexPtrEnd)
            {
                // horizontally draw the texture
                for (int i = 0; i < count; i++)
                {
                    uint* textureXPos = textureYPos_u + i;
                    uint texelIndex = (*textureXPos >> 16) & textureMask;
                    texelIndex += *(texturePos + i);

                    uint pixel = *(textureBuffer + texelIndex);

                    T.Draw(screenIndexPtr, pixel);

                    *textureXPos += *(textureYIncr_u + i);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        uint textureHeightMask = (uint)(textureHeight - 1);

        while (screenIndexPtr != screenIndexPtrEnd)
        {
            uint texelIndex = (textureYPos_u >> 16) & textureHeightMask;
            uint pixel = *(textureBuffer + texelIndex);

            T.Draw(screenIndexPtr, pixel);

            screenIndexPtr += width;
            textureYPos_u += textureYIncr_u;
        }

        return textureYPos_u;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        uint textureHeightMask = (uint)(textureHeight - 1);

        while (screenIndexPtr < screenIndexPtrEnd)
        {
            uint texelIndex = (textureYPos_u >> 16) & textureHeightMask;
            uint pixel = *(textureBuffer + texelIndex);

            T.Draw(screenIndexPtr, pixel);

            screenIndexPtr += width;
            textureYPos_u += textureYIncr_u;
        }
    }
}
