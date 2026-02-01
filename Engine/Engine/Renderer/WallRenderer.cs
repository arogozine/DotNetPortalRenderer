using RenderingEngine.Models;
using RenderingEngine.Tooling;
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

            using TempBuffer<uint> buffer = TempBuffer<uint>.GetBuffer(textureInfo.Height);

            Span<RenderColumnStatus> status = RenderWindowHelper.Status;
            Span<int> textureXLocation = RenderWindowHelper.TopTextureXLocation;
            Span<int> textureYLocation = RenderWindowHelper.TopTextureYLocation;
            Span<int> ceilingStart = RenderWindowHelper.CeilingStart;
            Span<int> wallStart = RenderWindowHelper.WallStart;
            Span<int> wallEnd = RenderWindowHelper.WallEnd;
            Span<int> floorEnd = RenderWindowHelper.FloorEnd;


            Span<int> clampedFrom = RenderWindowHelper.ClampedFrom;
            Span<int> clampedTo = RenderWindowHelper.ClampedTo;
            Span<int> textureXPosArray = RenderWindowHelper.TextureXPos;

            for (int x = wallFromX; x <= wallToX; x++)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    continue;
                }

                int clamptedFromY = clampedFrom[x];
                int clamptedToY = clampedTo[x];
                int textureXIncr = textureYLocation[x];
                int textureXPos = textureXPosArray[x];
                int textureYPos = textureXLocation[x];

                CalculateAndCacheWallColumn(buffer, ref wallTexturePtr, textureYPos, flipY);

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
            ref float angleCachePtr = ref memoryPool.GetBucketRef<float>(MemoryPoolBucket.AngleCache);

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
            angleX = MathFormulas.ClampAngle(angleX);

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

        private void PrecalculateBasicWallDistance(RenderablePortalWall renderableWall, Sector sector)
        {
            RenderableWall wall = renderableWall.Wall;

            PrecalculateWallDistanceShared2(renderableWall, sector, wall.MiddleTexture!, RenderWindowHelper.TopTextureXLocation, RenderWindowHelper.TopTextureYLocation);
        }

        private void PrecalculateWallDistanceShared2(
            RenderablePortalWall renderableWall, Sector sector, TextureInfo textureInfo,
            scoped Span<int> xLocation, scoped Span<int> yLocation)
        {
            Span<float> distance = RenderWindowHelper.Distance;
            Span<int> wallStart = RenderWindowHelper.WallStart;
            Span<int> wallEnd = RenderWindowHelper.WallEnd;

            Span<RenderColumnStatus> status = RenderWindowHelper.Status;
            Span<int> ceilingStart = RenderWindowHelper.CeilingStart;
            Span<int> floorEnd = RenderWindowHelper.FloorEnd;
            Span<int> clampedFrom = RenderWindowHelper.ClampedFrom;
            Span<int> clampedTo = RenderWindowHelper.ClampedTo;
            Span<int> textureXPos = RenderWindowHelper.TextureXPos;

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
            int textureStart = textureInfo.YOffset << 16;

            if (Vector.IsHardwareAccelerated && length > Vector<float>.Count)
            {
                Vector<int> canRenderWallMaskV = Vector.Create((int)(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderWall));

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

                Vector<int> textureStartV = Vector.Create(textureStart);
                Vector<int> finishedRendering = Vector.Create((int)RenderColumnStatus.FinishedRendering);

                Vector<float> cameraRayV = Vector.CreateSequence(cameraRay, cameraWidthIncr);
                Vector<float> cameraWidthIncrV = Vector.Create(cameraWidthIncr * Vector<float>.Count);

                int rem = (wallToX - wallFromX) % Vector<float>.Count;
                wallToX -= rem;

                bool textureHeightEven = SharedHelpers.IsPowerOfTwo(textureHeight);
                Vector<int> heightMask = textureHeightEven ? Vector.Create(textureHeight - 1) : default;

                for (int x = wallFromX; x < wallToX; x += Vector<float>.Count)
                {
                    Vector<int> columnStatusV = Vector.LoadUnsafe(ref statusInt[x]) & canRenderWallMaskV;

                    if (columnStatusV == Vector<int>.Zero)
                    {
                        Vector.StoreUnsafe(finishedRendering, ref statusInt[x]);
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

                    Vector<int> ceilingStartYV = Vector.LoadUnsafe(ref ceilingStart[x]);
                    Vector<int> floorEndYV = Vector.LoadUnsafe(ref floorEnd[x]);

                    Vector<int> clamptedFromYV = Vector.Clamp(wallStartV, ceilingStartYV, floorEndYV);
                    Vector<int> clamptedToYV = Vector.Clamp(wallEndV, ceilingStartYV, floorEndYV);
                    Vector<int> textureXPosV = textureStartV - topYLocationV * (wallStartV - clamptedFromYV);
                    textureXPosV = SharedHelpers.EnsureOffsetIsPositive(textureWidthV << 16, textureXPosV);

                    Vector.StoreUnsafe(clamptedFromYV, ref clampedFrom[x]);
                    Vector.StoreUnsafe(clamptedToYV, ref clampedTo[x]);
                    Vector.StoreUnsafe(textureXPosV, ref textureXPos[x]);

                    for (int i = 0; i < Vector<int>.Count; i++)
                    {
                        int clamptedFromY = clamptedFromYV[i];
                        int clamptedToY = clamptedToYV[i];
                        if (clamptedFromY >= clamptedToY)
                        {
                            statusInt[x + i] = (int)RenderColumnStatus.FinishedRendering;
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
                    status[x] = RenderColumnStatus.FinishedRendering;
                    continue;
                }

                (float fromToXdist, float fromToYdist) = MathFormulas.CalculateRayIntersection(cameraRay, t1, d2y, d2x);

                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];

                float distX = rX - fromToXdist;
                float distY = rY - fromToYdist;
                float textureDist = MathF.Sqrt(distX * distX + distY * distY);
                int textureXIncr = float.ConvertToIntegerNative<int>(scaledTextureWidth / (wallEndY - wallStartY));

                distance[x] = fromToYdist;
                xLocation[x] = float.ConvertToIntegerNative<int>(MathF.FusedMultiplyAdd(textureDist, xScale, xOffset));
                yLocation[x] = textureXIncr;
                xLocation[x] = (xLocation[x] % textureHeight) * textureWidth;

                int ceilingStartY = ceilingStart[x];
                int floorEndY = floorEnd[x];

                int clamptedFromY = Math.Clamp(wallStartY, ceilingStartY, floorEndY);
                int clamptedToY = Math.Clamp(wallEndY, ceilingStartY, floorEndY);
                int textureXPosY = textureStart - textureXIncr * (wallStartY - clamptedFromY);
                textureXPosY = SharedHelpers.EnsureOffsetIsPositive(textureWidth << 16, textureXPosY);

                clampedFrom[x] = clamptedFromY;
                clampedTo[x] = clamptedToY;
                textureXPos[x] = textureXPosY;

                if (clamptedFromY >= clamptedToY)
                {
                    status[x] = RenderColumnStatus.FinishedRendering;
                }
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
            int textureYPos, bool flipY)
        {
            // reuse the cached column
            if (tempBuffer.Index == textureYPos)
            {
                return;
            }

            tempBuffer.Index = textureYPos;

            Span<uint> buffer = tempBuffer.Span;
            ref BGRA columnPtr = ref Unsafe.Add(ref wallTexturePtr, textureYPos);

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
                    buffer[i] = columnPtr.Value;

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