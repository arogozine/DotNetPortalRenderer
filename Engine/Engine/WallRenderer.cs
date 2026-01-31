using RenderingEngine.Models;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref T GetScreenPtr<T>()
            where T : struct
        {
            return ref Unsafe.As<BGRA, T>(ref MemoryMarshal.GetReference(this.buffer));
        }

        private void CalculateDistance(RenderablePortalWall renderableWall)
        {
            int width = PixelWidth;
            var wall = renderableWall.Wall;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            Span<RenderColumnStatus> status = RenderWindowHelper.Status;
            Span<int> ceilingStart = RenderWindowHelper.CeilingStart;
            Span<int> floorEnd = RenderWindowHelper.FloorEnd;
            Span<float> distance = RenderWindowHelper.Distance;

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    continue;
                }

                (int portalFromYClamped, int portalToYClamped) = RenderWindowHelper.GetClampedWallFromTo(x);
                distance[x] = MathFormulas.CalculateDistance2(cameraRay, t1, d2y, d2x);
                ceilingStart[x] = portalFromYClamped;
                floorEnd[x] = portalToYClamped;
                status[x] ^= RenderColumnStatus.CanRenderWall;
            }
        }

        private bool DrawPortalWall(
            PortalPlayerSnapshot player,
            Sector sector,
            ReadOnlySpan<Sector> sectors,
            RenderablePortalWall renderableWall)
        {
            (int sectorHeight, int ceilOffset, int floorOffset) = CalculatePortalOffsets(sectors, renderableWall.Wall);

            // ceiling and floor of the sector are the same
            // so no wall is drawn
            if (floorOffset == 0 && ceilOffset == 0)
            {
                CalculateDistance(renderableWall);
                return true;
            }

            float oneOverSectorHeight = 1f / sectorHeight;
            byte lightLevel = sector.LightLevel;

            Span<RenderColumnStatus> status = RenderWindowHelper.Status;
            ReadOnlySpan<int> bottomTextureYLocation = RenderWindowHelper.BottomTextureYLocation;
            ReadOnlySpan<int> topTextureYLocation = RenderWindowHelper.TopTextureYLocation;
            Span<int> ceilingStart = RenderWindowHelper.CeilingStart;
            ReadOnlySpan<int> wallStart = RenderWindowHelper.WallStart;
            ReadOnlySpan<int> wallEnd = RenderWindowHelper.WallEnd;
            Span<int> floorEnd = RenderWindowHelper.FloorEnd;
            Span<int> bottomTextureXLocation = RenderWindowHelper.BottomTextureXLocation;
            Span<int> topTextureXLocation = RenderWindowHelper.TopTextureXLocation;

            if (floorOffset != 0 && ceilOffset != 0)
            {
                PrecalculatePortalWallDistance(sector, renderableWall);
            }
            else if (floorOffset != 0)
            {
                PrecalculateLowerWallDistance(sector, renderableWall);
            }
            else
            {
                PrecalculateUpperWallDistance(sector, renderableWall);
            }

            int width = PixelWidth;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            RenderableWall wall = renderableWall.Wall;
            TextureInfo upperTexture = wall.UpperTexture!;
            TextureInfo lowerTexture = wall.LowerTexture!;

            (bool upperSkybox, _, bool upperFlipY) = GetFlags(upperTexture);
            (bool lowerSkybox, _, bool lowerFlipY) = GetFlags(lowerTexture);

            ref BGRA upperTexturePtr = ref MemoryMarshal.GetReference(upperTexture.Texture.GetBinary(!upperSkybox, lightLevel));
            ref uint upperTextureUintPtr = ref Unsafe.As<BGRA, uint>(ref upperTexturePtr);

            ref BGRA lowerTexturePtr = ref MemoryMarshal.GetReference(lowerTexture.Texture.GetBinary(true, lightLevel));

            ref uint screenPtr = ref GetScreenPtr<uint>();

            using TempBuffer<uint> lowerBuffer = TempBuffer<uint>.GetBuffer(lowerTexture.Height);
            using TempBuffer<uint> upperBuffer = TempBuffer<uint>.GetBuffer(upperTexture.Height);

            ref float angleCachePtr = ref MemoryMarshal.GetArrayDataReference(angleCache);

            int lowerTextureStart = lowerTexture.YOffset << 16;
            int upperTextureStart = upperTexture.YOffset << 16;

            for (int x = wallFromX; x <= wallToX; x++)
            {
                RenderColumnStatus columnStatus = RenderWindowHelper.Status[x];

                if (!columnStatus.WallRenderable)
                {
                    status[x] = RenderColumnStatus.FinishedRendering;
                    continue;
                }

                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];
                int floorEndY = floorEnd[x];
                int ceilingStartY = ceilingStart[x];

                float pixelsPerHeight = (wallEndY - wallStartY) * oneOverSectorHeight;

                // Wall Calculation
                (int fromYClamped, int toYClamped) = RenderWindowHelper.GetClampedWallFromTo(x);

                // Portal Calculation
                int floorPixelOffset = float.ConvertToIntegerNative<int>(pixelsPerHeight * floorOffset);
                int ceilPixelOffset = float.ConvertToIntegerNative<int>(pixelsPerHeight * ceilOffset);
                int portalFromY = wallStartY - ceilPixelOffset;
                int portalToY = wallEndY - floorPixelOffset;
                int portalFromYClamped = Math.Clamp(portalFromY, ceilingStartY, floorEndY);
                int portalToYClamped = Math.Clamp(portalToY, ceilingStartY, floorEndY);

                // draw upper wall / upper skybox
                if (ceilOffset != 0 && fromYClamped < portalFromYClamped)
                {
                    if (upperSkybox)
                    {
                        ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, fromYClamped * width + x);
                        ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, portalFromYClamped * width + x);

                        RenderSkyboxLine(player,
                            x,
                            upperTexture,
                            ref upperTextureUintPtr,
                            ref angleCachePtr,
                            ref screenIndexPtr,
                            ref screenIndexPtrEnd);
                    }
                    else
                    {
                        int textureYPos = topTextureXLocation[x];
                        int textureXIncr = topTextureYLocation[x];

                        // Calculate Upper  Texture Position
                        int textureWidth = upperTexture.Height;
                        int textureXPos = upperTextureStart - textureXIncr * (wallStartY - fromYClamped);

                        textureXPos = SharedHelpers.EnsureOffsetIsPositive(textureWidth << 16, textureXPos);

                        CalculateAndCacheWallColumn(upperBuffer, ref upperTexturePtr, textureYPos, lightLevel, upperFlipY);

                        RenderWallLine(
                            width,
                            x,
                            textureWidth,
                            fromYClamped,
                            portalFromYClamped,
                            (uint)textureXPos,
                            (uint)textureXIncr,
                            ref screenPtr,
                            ref upperBuffer.Pointer
                        );
                    }
                }

                // draw lower wall
                if (floorOffset != 0 && portalToYClamped < toYClamped)
                {
                    if (lowerSkybox)
                    {
                        ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, portalToYClamped * width + x);
                        ref readonly uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, toYClamped * width + x);

                        RenderSkyboxLine(player,
                            x,
                            upperTexture,
                            ref upperTextureUintPtr,
                            ref angleCachePtr,
                            ref screenIndexPtr,
                            in screenIndexPtrEnd);
                    }
                    else
                    {
                        int textureYPos = bottomTextureXLocation[x];
                        int textureXIncr = bottomTextureYLocation[x];

                        int textureWidth = lowerTexture.Height;

                        int textureXPos = textureXIncr * (portalToYClamped - portalToY) + lowerTextureStart;

                        textureXPos = SharedHelpers.EnsureOffsetIsPositive(textureWidth << 16, textureXPos);

                        CalculateAndCacheWallColumn(lowerBuffer, ref lowerTexturePtr, textureYPos, lightLevel, lowerFlipY);

                        RenderWallLine(
                            width,
                            x,
                            textureWidth,
                            portalToYClamped,
                            toYClamped,
                            (uint)textureXPos,
                            (uint)textureXIncr,
                            ref screenPtr,
                            ref lowerBuffer.Pointer
                        );
                    }
                }

                ceilingStart[x] = portalFromYClamped;
                floorEnd[x] = portalToYClamped;
                status[x] ^= RenderColumnStatus.CanRenderWall;
            }

            // if sector height matches top or bottom offset only top or bottom texture was drawn
            // no middle texture is possible, thus we can treat this as basic wall
            return !(floorOffset == sectorHeight || sectorHeight == -ceilOffset);
        }

        private bool DrawBasicWall(
            PortalPlayerSnapshot player,
            Sector sector,
            RenderablePortalWall renderableWall)
        {
            // separate path for skybox rendering
            RenderableWall wall = renderableWall.Wall;
            TextureInfo textureInfo = wall.MiddleTexture!;

            (bool skybox, _, bool flipY) = GetFlags(textureInfo);

            if (skybox)
            {
                return DrawBasicSkyboxWall(player, renderableWall);
            }

            PrecalculateBasicWallDistance(renderableWall, sector);

            int width = PixelWidth;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            byte lightLevel = sector.LightLevel;

            ref uint screenPtr = ref GetScreenPtr<uint>();            

            ref BGRA wallTexturePtr = ref MemoryMarshal.GetReference(textureInfo.Texture.GetBinary(true, lightLevel));
            int textureWidth = textureInfo.Height;

            int textureStart = textureInfo.YOffset << 16;
            using TempBuffer<uint> buffer = TempBuffer<uint>.GetBuffer(textureInfo.Height);

            Span<RenderColumnStatus> status = RenderWindowHelper.Status;
            Span<int> textureXLocation = RenderWindowHelper.TopTextureXLocation;
            Span<int> textureYLocation = RenderWindowHelper.TopTextureYLocation;
            Span<int> ceilingStart = RenderWindowHelper.CeilingStart;
            Span<int> wallStart = RenderWindowHelper.WallStart;
            Span<int> wallEnd = RenderWindowHelper.WallEnd;
            Span<int> floorEnd = RenderWindowHelper.FloorEnd;

            int length = (wallToX - wallFromX) - Vector<int>.Count;

            if (length > Vector<int>.Count)
            {
                Span<int> statusInt = MemoryMarshal.Cast<RenderColumnStatus, int>(status);
                Vector<int> canRenderWallMaskV = Vector.Create((int)(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderWall));
                Vector<int> textureStartV = Vector.Create(textureStart);
                Vector<int> finishedRendering = Vector.Create((int)RenderColumnStatus.FinishedRendering);

                int rem = length % Vector<int>.Count;
                wallToX -= rem;

                for (int x = wallFromX; x < wallToX; x += Vector<int>.Count)
                {
                    Vector<int> columnStatusV = Vector.LoadUnsafe(ref statusInt[x]) & canRenderWallMaskV;

                    if (columnStatusV == Vector<int>.Zero)
                    {
                        Vector.StoreUnsafe(finishedRendering, ref statusInt[x]);
                        continue;
                    }

                    Vector<int> textureYPosV = Vector.LoadUnsafe(ref textureXLocation[x]);
                    Vector<int> textureXIncrV = Vector.LoadUnsafe(ref textureYLocation[x]);
                    Vector<int> wallStartYV = Vector.LoadUnsafe(ref wallStart[x]);
                    Vector<int> wallEndYV = Vector.LoadUnsafe(ref wallEnd[x]);
                    Vector<int> ceilingStartYV = Vector.LoadUnsafe(ref ceilingStart[x]);
                    Vector<int> floorEndYV = Vector.LoadUnsafe(ref floorEnd[x]);

                    Vector<int> clamptedFromYV = Vector.Clamp(wallStartYV, ceilingStartYV, floorEndYV);
                    Vector<int> clamptedToYV = Vector.Clamp(wallEndYV, ceilingStartYV, floorEndYV);
                    Vector<int> textureXPosV = textureStartV - textureXIncrV * (wallStartYV - clamptedFromYV);

                    Span<byte> a = new byte[Vector<int>.Count];
                    Span<byte> b = new byte[Vector<int>.Count];

                    bool repeat =
                        PopulateRepeatedValues(a, textureYPosV) &&
                        PopulateRepeatedValues(b, textureXPosV) &&
                        RefineRepeatedValues(a, b) &&
                        PopulateRepeatedValues(b, textureXIncrV) &&
                        RefineRepeatedValues(a, b) &&
                        PopulateRepeatedValues(b, clamptedFromYV) &&
                        RefineRepeatedValues(a, b) &&
                        PopulateRepeatedValues(b, clamptedToYV) &&
                        RefineRepeatedValues(a, b);

                    for (int j = 0; j < Vector<int>.Count; )
                    {
                        if (columnStatusV[j] == 0)
                        {
                            j++;
                            continue;
                        }

                        int clamptedFromY = clamptedFromYV[j];
                        int clamptedToY = clamptedToYV[j];

                        if (clamptedFromY >= clamptedToY)
                        {
                            j++;
                            continue;
                        }

                        int textureXPos = textureXPosV[j];
                        int textureYPos = textureYPosV[j];
                        int textureXIncr = textureXIncrV[j];

                        CalculateAndCacheWallColumn(buffer, ref wallTexturePtr, textureYPos, lightLevel, flipY);

                        textureXPos = SharedHelpers.EnsureOffsetIsPositive(textureWidth << 16, textureXPos);

                        byte count = a[j];

                        if (repeat && count > 1)
                        {                            
                            RenderWallLine(
                                    count,
                                    width,
                                    x + j,
                                    textureWidth,
                                    clamptedFromY,
                                    clamptedToY,
                                    (uint)textureXPos,
                                    (uint)textureXIncr,
                                    ref screenPtr,
                                    ref buffer.Pointer
                                );

                            j += count - 1;
                        }
                        else
                        {
                            RenderWallLine(
                                width,
                                x + j,
                                textureWidth,
                                clamptedFromY,
                                clamptedToY,
                                (uint)textureXPos,
                                (uint)textureXIncr,
                                ref screenPtr,
                                ref buffer.Pointer
                            );
                        }

                        j++;
                    }


                    Vector.StoreUnsafe(finishedRendering, ref statusInt[x]);
                }

                wallFromX = wallToX;
                wallToX += rem;
            }

            for (int x = wallFromX; x <= wallToX; x++)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    status[x] = RenderColumnStatus.FinishedRendering;
                    continue;
                }

                int textureXIncr = textureYLocation[x];
                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];
                int ceilingStartY = ceilingStart[x];
                int floorEndY = floorEnd[x];

                int clamptedFromY = Math.Clamp(wallStartY, ceilingStartY, floorEndY);
                int clamptedToY = Math.Clamp(wallEndY, ceilingStartY, floorEndY);
                int textureXPos = textureStart - textureXIncr * (wallStartY - clamptedFromY);

                if (clamptedFromY >= clamptedToY)
                {
                    status[x] = RenderColumnStatus.FinishedRendering;
                    continue;
                }

                int textureYPos = textureXLocation[x];

                CalculateAndCacheWallColumn(buffer, ref wallTexturePtr, textureYPos, lightLevel, flipY);

                textureXPos = SharedHelpers.EnsureOffsetIsPositive(textureWidth << 16, textureXPos);

                RenderWallLine(
                    width,
                    x,
                    textureWidth,
                    clamptedFromY,
                    clamptedToY,
                    (uint)textureXPos,
                    (uint)textureXIncr,
                    ref screenPtr,
                    ref buffer.Pointer
                );

                status[x] = RenderColumnStatus.FinishedRendering;
            }

            return true;
        }

        private static bool PopulateRepeatedValues(scoped Span<ushort> repeatedCount, scoped ReadOnlySpan<int> values)
        {
            bool repeated = false;

            for (int i = 0; i < values.Length; )
            {
                ushort c = 1;
                int l = values[i];

                for (int j = i + 1; j < values.Length; j++)
                {
                    int next = values[j];

                    if (l == next)
                    {
                        c++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (c == 0)
                {
                    repeatedCount[i] = 0;
                    i++;
                    continue;
                }

                repeated = true;

                for (; c > 0; c--, i++)
                {
                    repeatedCount[i] = c;
                }
            }

            return repeated;
        }

        private static bool PopulateRepeatedValues(scoped Span<byte> repeatedCount, Vector<int> values)
        {
            bool repeated = false;

            for (int i = 0; i < Vector<int>.Count;)
            {
                byte c = 1;
                int l = values[i];

                for (int j = i + 1; j < Vector<int>.Count; j++)
                {
                    int next = values[j];

                    if (l == next)
                    {
                        c++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (c == 0)
                {
                    repeatedCount[i] = 0;
                    i++;
                    continue;
                }

                repeated = true;

                for (; c > 0; c--, i++)
                {
                    repeatedCount[i] = c;
                }
            }

            return repeated;
        }


        private static bool RefineRepeatedValues(
            scoped Span<byte> a,
            scoped ReadOnlySpan<byte> b)
        {
            bool repeated = false;

            for (int i = 0; i < a.Length;)
            {
                byte repeat_a = a[i];

                if (repeat_a == 0)
                {
                    i++;
                    continue;
                }

                byte repeat_b = b[i];

                if (repeat_b == 0)
                {
                    a[i] = 0;
                    i++;
                    continue;
                }

                if (repeat_a == repeat_b)
                {
                    i += repeat_a;
                    continue;
                }

                repeat_a = repeat_b < repeat_a ? repeat_b : repeat_a;

                for (; repeat_a > 0 && i < a.Length; repeat_a--, i++)
                {
                    a[i] = repeat_a;
                }

                // i += skip;
                repeated = true;
            }

            return repeated;
        }

        private static bool AccountForHoles(scoped Span<ushort> array, int length)
        {
            if (array.Length == 1)
            {
                return array[0] != 0;
            }

            bool renderable = false;
            int j = 0;

            for (int i = 0; i < array.Length; i++)
            {
                ushort val = array[i];

                if (val == length)
                {
                    j++;
                    renderable = true;
                    continue;
                }

                int start = i - j;
                for (int s = start; s < i; s++)
                {
                    array[s] = (ushort)(j - s + start);
                }

                j = 0;
            }

            if (j != length)
            {
                int start = length - j;
                for (int s = start; s < length; s++)
                {
                    array[s] = (ushort)(j - s + start);
                }
            }

            return renderable;
        }

        private static bool RefineRepeatedValues(
            scoped Span<ushort> a,
            scoped ReadOnlySpan<ushort> b)
        {
            bool repeated = false;

            for (int i = 0; i < a.Length;)
            {
                ushort repeat_a = a[i];

                if (repeat_a == 0)
                {
                    i++;
                    continue;
                }

                ushort repeat_b = b[i];

                if (repeat_b == 0)
                {
                    a[i] = 0;
                    i++;
                    continue;
                }

                if (repeat_a == repeat_b)
                {
                    i += repeat_a;
                    continue;
                }

                repeat_a = repeat_b < repeat_a ? repeat_b : repeat_a;

                for (; repeat_a > 0 && i < a.Length; repeat_a--, i++)
                {
                    a[i] = repeat_a;
                }

                // i += skip;
                repeated = true;
            }

            return repeated;
        }


        private bool DrawBasicSkyboxWall(
            PortalPlayerSnapshot player,
            RenderablePortalWall renderableWall)
        {
            Span<RenderColumnStatus> status = RenderWindowHelper.Status;
            Span<float> distance = RenderWindowHelper.Distance;

            int width = PixelWidth;
            RenderableWall wall = renderableWall.Wall;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            TextureInfo wallTexture = wall.MiddleTexture!;
            ref uint wallTextureUintPtr = ref Unsafe.As<BGRA, uint>(ref MemoryMarshal.GetReference(wallTexture.Texture.GetBinary(false, 0)));
            ref float angleCachePtr = ref MemoryMarshal.GetArrayDataReference(angleCache);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    distance[x] = MathFormulas.CalculateDistance2(cameraRay, t1, d2y, d2x);
                    status[x] = RenderColumnStatus.FinishedRendering;
                    continue;
                }

                (int clamptedFromY, int clamptedToY) = RenderWindowHelper.GetClampedWallFromTo(x);

                ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, clamptedFromY * width + x);
                ref uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, clamptedToY * width + x);

                float fromToYdist = MathFormulas.CalculateDistance2(cameraRay, t1, d2y, d2x);

                RenderSkyboxLine(player,
                    x,
                    wallTexture,
                    ref wallTextureUintPtr,
                    ref angleCachePtr,
                    ref screenIndexPtr,
                    ref screenIndexPtrEnd);

                distance[x] = fromToYdist;
                status[x] = RenderColumnStatus.FinishedRendering;
            }

            return true;
        }

        #region Render Line

        private void RenderSkyboxLine(PortalPlayerSnapshot player,
            int x,
            TextureInfo upperTexture,
            ref uint upperTextureUintPtr,
            ref float angleCachePtr,
            ref uint screenIndexPtr,
            ref readonly uint screenIndexPtrEnd)
        {
            const float twoPi = 2 * MathF.PI;
            const float oneOverTwoPi = 1f / (2 * MathF.PI);

            int width = PixelWidth;
            int height = PixelHeight;

            float viewAngle = player.Angle;

            int textureWidth = upperTexture.Width;
            int textureHeight = upperTexture.Height;

            float textureWidth4 = textureWidth * 4f * oneOverTwoPi;
            float yTextureIncr = (1f / height) * textureHeight;

            // calculate angle between 0 to 2 PI
            float angleX = Unsafe.Add(ref angleCachePtr, x) - viewAngle;
            if (angleX > twoPi)
            {
                angleX -= twoPi;
            }
            else if (angleX < 0f)
            {
                angleX = twoPi + angleX;
            }

            int texX = float.ConvertToIntegerNative<int>(textureWidth4 * angleX) % textureWidth;

            int wallStart = RenderWindowHelper.WallStart[x];
            int ceilingStart = RenderWindowHelper.CeilingStart[x];
            int floorEnd = RenderWindowHelper.FloorEnd[x];
            int fromYClamped = Math.Clamp(wallStart, ceilingStart, floorEnd);
            float vScreen = fromYClamped * yTextureIncr;

            ref uint textureColumnPtr = ref Unsafe.Add(ref upperTextureUintPtr, texX);

            for (;
                    Unsafe.IsAddressGreaterThan(in screenIndexPtrEnd, in screenIndexPtr);
                    vScreen += yTextureIncr, screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width)
                )
            {
                int index = textureWidth * float.ConvertToIntegerNative<int>(vScreen);
                screenIndexPtr = Unsafe.Add(ref textureColumnPtr, index);
            }
        }

        private static void RenderWallLine(
            byte count,
            int width,
            int x,
            int textureHeight,
            int startY,
            int endY,
            uint textureXPos_u,
            uint textureXIncr_u,
            scoped ref uint screenPtr,
            scoped ref uint textureBuffer
        )
        {
            ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, startY * width + x);
            ref readonly uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, endY * width + x);

            // % is slower than the bitwise &
            // thus we have two paths to render a wall line
            // depending if texture is power of two or not
            if (SharedHelpers.IsPowerOfTwo(textureHeight))
            {
                uint textureMask = (uint)(textureHeight - 1);

                while (!Unsafe.AreSame(in screenIndexPtr, in screenIndexPtrEnd))
                {
                    uint texelIndex = (textureXPos_u >> 16) & textureMask;
                    uint shaded = Unsafe.Add(ref textureBuffer, texelIndex);
                    for (int i = 0; i <= count; i++)
                    {
                        Unsafe.Add(ref screenIndexPtr, i) = shaded;
                    }
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width);
                    textureXPos_u += textureXIncr_u;
                }
            }
            else
            {
                uint textureHeight_u = (uint)textureHeight;

                while (Unsafe.IsAddressLessThan(in screenIndexPtr, in screenIndexPtrEnd))
                {
                    uint texelIndex = (textureXPos_u >> 16) % textureHeight_u;
                    uint shaded = Unsafe.Add(ref textureBuffer, texelIndex);

                    for (int i = 0; i < count; i++)
                    {
                        Unsafe.Add(ref screenIndexPtr, i) = shaded;
                    }
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width);
                    textureXPos_u += textureXIncr_u;
                }
            }
        }

        private static void RenderWallLine(
            int width,
            int x,
            int textureHeight,
            int startY,
            int endY,
            uint textureXPos_u,
            uint textureXIncr_u,
            scoped ref uint screenPtr,
            scoped ref uint textureBuffer
            )
        {
            ref uint screenIndexPtr = ref Unsafe.Add(ref screenPtr, startY * width + x);
            ref readonly uint screenIndexPtrEnd = ref Unsafe.Add(ref screenPtr, endY * width + x);

            // % is slower than the bitwise &
            // thus we have two paths to render a wall line
            // depending if texture is power of two or not
            if (SharedHelpers.IsPowerOfTwo(textureHeight))
            {
                uint textureMask = (uint)(textureHeight - 1);

                while (!Unsafe.AreSame(in screenIndexPtr, in screenIndexPtrEnd))
                {
                    uint texelIndex = (textureXPos_u >> 16) & textureMask;
                    uint shaded = Unsafe.Add(ref textureBuffer, texelIndex);

                    screenIndexPtr = shaded;
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width);
                    textureXPos_u += textureXIncr_u;
                }
            }
            else
            {
                uint textureHeight_u = (uint)textureHeight;

                while (Unsafe.IsAddressLessThan(in screenIndexPtr, in screenIndexPtrEnd))
                {
                    uint texelIndex = (textureXPos_u >> 16) % textureHeight_u;
                    uint shaded = Unsafe.Add(ref textureBuffer, texelIndex);

                    screenIndexPtr = shaded;
                    screenIndexPtr = ref Unsafe.Add(ref screenIndexPtr, width);
                    textureXPos_u += textureXIncr_u;
                }
            }
        }

        #endregion

        #region Pre Calculate

        private void PrecalculateUpperWallDistance(
            Sector sector,
            RenderablePortalWall renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;

            PrecalculateWallDistanceShared(renderableWall, sector, wall.UpperTexture!, RenderWindowHelper.TopTextureXLocation, RenderWindowHelper.TopTextureYLocation);
        }

        private void PrecalculateLowerWallDistance(
            Sector sector,
            RenderablePortalWall renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;

            PrecalculateWallDistanceShared(renderableWall, sector, wall.LowerTexture!, RenderWindowHelper.BottomTextureXLocation, RenderWindowHelper.BottomTextureYLocation);
        }

        private void PrecalculatePortalWallDistance(
            Sector sector,
            RenderablePortalWall renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;

            PrecalculateWallDistanceShared(renderableWall, sector, wall.LowerTexture!, RenderWindowHelper.BottomTextureXLocation, RenderWindowHelper.BottomTextureYLocation);
            PrecalculateWallDistanceShared(renderableWall, sector, wall.UpperTexture!, RenderWindowHelper.TopTextureXLocation, RenderWindowHelper.TopTextureYLocation);
        }

        private void PrecalculateBasicWallDistance(RenderablePortalWall renderableWall, Sector sector)
        {
            RenderableWall wall = renderableWall.Wall;

            PrecalculateWallDistanceShared(renderableWall, sector, wall.MiddleTexture!, RenderWindowHelper.TopTextureXLocation, RenderWindowHelper.TopTextureYLocation);
        }

        private void PrecalculateWallDistanceShared(
            RenderablePortalWall renderableWall, Sector sector, TextureInfo textureInfo,
            scoped Span<int> xLocation, scoped Span<int> yLocation)
        {
            Span<float> distance = RenderWindowHelper.Distance;
            Span<int> wallStart = RenderWindowHelper.WallStart;
            Span<int> wallEnd = RenderWindowHelper.WallEnd;
            Span<RenderColumnStatus> status = RenderWindowHelper.Status;

            RenderableWall wall = renderableWall.Wall;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int width = PixelWidth;
            bool flipX = textureInfo.RenderingOptions.IsFlippedX;
            flipX = wall.Flipped ? !flipX : flipX;
            float xOffset = textureInfo.XOffset;

            (int textureWidth, int textureHeight, float xScale, float scaledTextureWidth) = CalculateScale(sector, wall, textureInfo);

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);

            float rX = flipX ? wall.R2.X : wall.R1.X;
            float rY = flipX ? wall.R2.Y : wall.R1.Y;

            int length = (wallToX - wallFromX);

            if (Vector.IsHardwareAccelerated && length > Vector<float>.Count)
            {
                const int canRenderWallMask = (int)(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderWall);

                Vector<int> canRenderWallMaskV = Vector.Create(canRenderWallMask);

                Span<int> statusInt = MemoryMarshal.Cast<RenderColumnStatus, int>(status);

                Vector<float> t1V = Vector.Create(t1);
                Vector<float> d2yV = Vector.Create(d2y);
                Vector<float> d2xV = Vector.Create(d2x);
                Vector<float> rXV = Vector.Create(rX);
                Vector<float> rYV = Vector.Create(rY);

                Vector<float> xScaleV = Vector.Create(xScale);
                Vector<float> xOffsetV = Vector.Create(xOffset);
                Vector<float> scaledTextureWidthV = Vector.Create(scaledTextureWidth);
                Vector<int> textureWidthV = Vector.Create(textureWidth);


                Vector<float> cameraRayV = Vector.CreateSequence(cameraRay, cameraWidthIncr);
                Vector<float> cameraWidthIncrV = Vector.Create(cameraWidthIncr * Vector<float>.Count);

                int rem = (wallToX - wallFromX) % Vector<float>.Count;
                wallToX -= rem;

                bool textureHeightEven = SharedHelpers.IsPowerOfTwo(textureHeight);
                Vector<int> heightMask = textureHeightEven ? Vector.Create(textureHeight - 1) : default;

                for (int x = wallFromX; x < wallToX; x += Vector<float>.Count)
                {
                    Vector<int> columnStatusV = Vector.LoadUnsafe(ref statusInt[x]);

                    if ((columnStatusV & canRenderWallMaskV) == Vector<int>.Zero)
                    {
                        continue;
                    }

                    (Vector<float> fromToXdist, Vector<float> fromToYdist) = MathFormulas.CalculateRayIntersection(cameraRayV, t1V, d2yV, d2xV);
                    Vector<float> distX = rXV - fromToXdist;
                    Vector<float> distY = rYV - fromToYdist;

                    Vector<int> wallStartV = Vector.LoadUnsafe(ref wallStart[x]);
                    Vector<int> wallEndV = Vector.LoadUnsafe(ref wallEnd[x]);

                    Vector<float> textureDist = Vector.SquareRoot(distX * distX + distY * distY);
                    Vector<int> topXLocationV = Vector.ConvertToInt32Native(Vector.FusedMultiplyAdd(textureDist, xScaleV, xOffsetV));
                    Vector<int> topYLocationV = Vector.ConvertToInt32Native(scaledTextureWidthV / Vector.ConvertToSingle(wallEndV - wallStartV));

                    Vector.StoreUnsafe(fromToYdist, ref distance[x]);
                    Vector.StoreUnsafe(topYLocationV, ref yLocation[x]);

                    if (textureHeightEven)
                    {
                        topXLocationV = (topXLocationV & heightMask) * textureWidthV;
                        Vector.StoreUnsafe(topXLocationV, ref xLocation[x]);
                    }
                    else
                    {
                        for (int i = 0; i < Vector<float>.Count; i++)
                        {
                            xLocation[x + i] = (topXLocationV[i] % textureHeight) * textureWidth;
                        }
                    }

                    cameraRayV += cameraWidthIncrV;
                }

                wallFromX = wallToX;
                wallToX += rem;
                cameraRay = cameraRayV[0];
            }

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    continue;
                }

                (float fromToXdist, float fromToYdist) = MathFormulas.CalculateRayIntersection(cameraRay, t1, d2y, d2x);

                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];

                float distX = rX - fromToXdist;
                float distY = rY - fromToYdist;
                float textureDist = MathF.Sqrt(distX * distX + distY * distY);

                distance[x] = fromToYdist;
                xLocation[x] = float.ConvertToIntegerNative<int>(MathF.FusedMultiplyAdd(textureDist, xScale, xOffset));
                yLocation[x] = float.ConvertToIntegerNative<int>(scaledTextureWidth / (wallEndY - wallStartY));
                xLocation[x] = (xLocation[x] % textureHeight) * textureWidth;
            }
        }

        private static (int Width, int Height, float XScale, float ScaledTextureWidth) CalculateScale(
            Sector sector,
            RenderableWall wall,
            TextureInfo wallTexture)
        {
            int textureWidth = wallTexture.Height;
            int textureHeight = wallTexture.Width;

            if (wallTexture.XScale is float xScale)
            {
                float wallLength = wall.Length;
                xScale = xScale / wallLength * textureHeight;
            }
            else
            {
                xScale = 1f;
            }

            float scaledTextureWidth;

            if (wallTexture.YScale is float yScale)
            {
                yScale = (sector.Ceil - sector.Floor) * yScale;
                scaledTextureWidth = ((textureWidth << 16) * yScale);
            }
            else
            {
                scaledTextureWidth = (sector.Ceil - sector.Floor) << 16;
            }

            return (textureWidth, textureHeight, xScale, scaledTextureWidth);
        }

        #endregion

        #region Calculation Helpers

        private static (int SectorHeight, int CeilingOffset, int FloorOffset) CalculatePortalOffsets(ReadOnlySpan<Sector> sectors, RenderableWall wall)
        {
            Sector sector = wall.Sector;
            Sector neighborSector = sectors[wall.Neighbor];
            int sectorHeight = sector.Ceil - sector.Floor;
            int floorOffset = neighborSector.Floor - sector.Floor;
            int ceilOffset = neighborSector.Ceil - sector.Ceil;

            if (floorOffset < 0)
            {
                floorOffset = 0;
            }

            if (ceilOffset > 0)
            {
                ceilOffset = 0;
            }

            // don't draw beyond the bounds
            if (ceilOffset < -sectorHeight)
            {
                ceilOffset = -sectorHeight;
            }

            if (floorOffset > sectorHeight)
            {
                floorOffset = sectorHeight;
            }

            return (sectorHeight, ceilOffset, floorOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalculateAndCacheWallColumn(
            TempBuffer<uint> tempBuffer,
            scoped ref BGRA wallTexturePtr,
            int textureYPos, byte brightness, bool flipY)
        {
            // reuse the cached column
            if (tempBuffer.Index == textureYPos)
            {
                return;
            }

            tempBuffer.Index = textureYPos;

            Span<uint> buffer = tempBuffer.Span;
            ref BGRA columnPtr = ref Unsafe.Add(ref wallTexturePtr, textureYPos);
            uint scale = brightness;

            if (flipY)
            {
                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    buffer[i] = columnPtr.Value;
                    columnPtr = ref Unsafe.Add(ref columnPtr, 1);
                }
            }
            else
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = columnPtr.Value; //  b | g | r | Alpha;

                    columnPtr = ref Unsafe.Add(ref columnPtr, 1);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (bool IsSkybox, bool FlipX, bool FlipY) GetFlags(TextureInfo textureInfo)
        {
            if (textureInfo is null)
            {
                return (false, false, false);
            }

            TextureRenderingOptions options = textureInfo.RenderingOptions;

            bool skyBox = options.HasFlag(TextureRenderingOptions.Skybox);
            bool flipX = options.HasFlag(TextureRenderingOptions.FlipX);
            bool flipY = options.HasFlag(TextureRenderingOptions.FlipY);

            return (skyBox, flipX, flipY);
        }

        #endregion
    }
}