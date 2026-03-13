using RenderingEngine.Models;
using RenderingEngine.Tooling;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe ref T GetScreenPtr<T>()
            where T : unmanaged
        {
            return ref Unsafe.AsRef<T>(buffer);
        }

        private bool DrawBasicWall(
            PortalPlayerSnapshot player,
            RenderablePortalWall renderableWall)
        {
            // separate path for skybox rendering
            RenderableWall wall = renderableWall.Wall;
            TextureInfo textureInfo = wall.MiddleTexture!;

            Debug.Assert(textureInfo != null);

            if (textureInfo.RenderingOptions.IsSkybox)
            {
                return DrawBasicSkyboxWall(player, renderableWall);
            }

            // Precalculate render window and texture positions
            PrecalculateBasicWallDistance(renderableWall);

            ref RenderColumnStatus statusRef = ref memoryPool.GetBucketRef<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            ushort length = (ushort)(wallToX - wallFromX + 1);
            Span<ushort> repeatedCount = TempBuffer<ushort>.GetBuffer(length);
            repeatedCount.Fill(length);

            // Set the X position as "FinishedRendering" for this wall
            // and determines where there repeat count is 0 (nothing to draw)
            for (int x = wallFromX; x <= wallToX; x++)
            {
                ref RenderColumnStatus columnStatus = ref Unsafe.Add(ref statusRef, x);

                if (!columnStatus.WallRenderable)
                {
                    repeatedCount[x - wallFromX] = 0;
                }

                columnStatus = RenderColumnStatus.FinishedRendering;
            }

            Span<uint> textureXLocation = memoryPool.GetBucket<uint>(MemoryPoolBucket.TopTextureXLocation);
            Span<uint> topTextureYIncrement = memoryPool.GetBucket<uint>(MemoryPoolBucket.TopTextureYIncrement);
            Span<uint> wallStartClamped = memoryPool.GetBucket<uint>(MemoryPoolBucket.WallStart);
            Span<uint> wallEndClamped = memoryPool.GetBucket<uint>(MemoryPoolBucket.WallEnd);

            DrawWallShared(renderableWall, wall.MiddleTexture!, repeatedCount, textureXLocation, topTextureYIncrement, wallStartClamped, wallEndClamped);

            return true;
        }

        private bool DrawBasicSkyboxWall(
            PortalPlayerSnapshot player,
            RenderablePortalWall renderableWall)
        {
            Span<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            Span<float> distance = memoryPool.GetBucket<float>(MemoryPoolBucket.Distance);
            Span<int> ceilingStartSpan = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            Span<int> wallStartSpan = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped);
            Span<int> wallEndSpan = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped);
            Span<int> floorEndSpan = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);

            int width = PixelWidth;
            RenderableWall wall = renderableWall.Wall;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            TextureInfo wallTexture = wall.MiddleTexture!;
            ref uint wallTextureUintPtr = ref wallTexture.Texture.GetBinaryRef<uint>(false, 0);
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

                int ceilingStart = ceilingStartSpan[x];
                int wallStartY = wallStartSpan[x];
                int wallEndY = wallEndSpan[x];
                int floorEndY = floorEndSpan[x];

                int clamptedFromY = Math.Clamp(wallStartY, ceilingStart, floorEndY);
                int clamptedToY = Math.Clamp(wallEndY, ceilingStart, floorEndY);

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
            var wallStart = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped);
            var ceilingStart = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            var floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);

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

            int wallStartY = wallStart[x];
            int ceilingStartY = ceilingStart[x];
            int floorEndY = floorEnd[x];
            int fromYClamped = Math.Clamp(wallStartY, ceilingStartY, floorEndY);
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

        #endregion

        #region Pre Calculate

        private void PrecalculateBasicWallDistance(RenderablePortalWall renderableWall)
        {
            Debug.Assert(renderableWall.Wall.MiddleTexture != null);

            Span<int> xLocation = memoryPool.GetBucket<int>(MemoryPoolBucket.TopTextureXLocation);

            CalculateRenderWindow2(true, renderableWall, renderableWall.Wall.MiddleTexture);
            CalculateWallClamp(renderableWall);
            CalculateTextureDistanceAndXPosition(xLocation, renderableWall, renderableWall.Wall.MiddleTexture!);
            CalculateTextureYStartAndIncrement(true, renderableWall, renderableWall.Wall.MiddleTexture!);
        }

        private static (int Height, int Width, float XScale, float ScaledTextureHeight) CalculateScale(
            Sector sector,
            RenderableWall wall,
            TextureInfo wallTexture)
        {
            int textureHeight = wallTexture.Height;
            int textureWidth = wallTexture.Width;

            if (wallTexture.XScale is float xScale)
            {
                float wallLength = wall.Length;
                xScale = xScale / wallLength * textureWidth;
            }
            else
            {
                xScale = 1f;
            }

            float scaledTextureHeight;

            if (wallTexture.YScale is float yScale)
            {
                yScale = (sector.Ceil - sector.Floor) * yScale;
                scaledTextureHeight = ((textureHeight << 16) * yScale);
            }
            else
            {
                scaledTextureHeight = (sector.Ceil - sector.Floor) << 16;
            }

            return (textureHeight, textureWidth, xScale, scaledTextureHeight);
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
            scoped ref uint wallTexturePtr,
            int textureYPos, bool flipY)
        {
            // reuse the cached column
            if (tempBuffer.Index == textureYPos)
            {
                return;
            }

            tempBuffer.Index = textureYPos;

            Span<uint> buffer = tempBuffer.Span;
            ref uint columnPtr = ref Unsafe.Add(ref wallTexturePtr, textureYPos);

            if (flipY)
            {
                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    buffer[i] = columnPtr;
                    columnPtr = ref Unsafe.Add(ref columnPtr, 1);
                }
            }
            else
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = columnPtr;
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