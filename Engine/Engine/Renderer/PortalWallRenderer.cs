using RenderingEngine.Models;
using RenderingEngine.Tooling;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
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
            bool renderLower = floorOffset != 0;
            bool renderUpper = ceilOffset != 0;

            // ceiling and floor of the sector are the same
            // so no wall is drawn
            if (!renderLower && !renderUpper)
            {
                CalculateDistance(renderableWall);
                return true;
            }

            RenderableWall wall = renderableWall.Wall;

            if (renderUpper)
            {
                TextureInfo upperTexture = wall.UpperTexture!;
                PrecalculateUpperWallDistance(sector, renderableWall);

                if (upperTexture.RenderingOptions.IsSkybox)
                {
                    DrawUpperSkyboxPortalWall(player, sectors, renderableWall);
                }
                else
                {
                    DrawUpperPortalWall(sectors, renderableWall);
                }
            }

            if (renderLower)
            {
                TextureInfo lowerTexture = wall.LowerTexture!;
                PrecalculateLowerWallDistance(sector, renderableWall);

                if (lowerTexture.RenderingOptions.IsSkybox)
                {
                    DrawLowerSkyboxPortalWall(player, sectors, renderableWall);
                }
                else
                {
                    DrawLowerPortalWall(sectors, renderableWall);
                }
            }

            Span<RenderColumnStatus> status = RenderWindowHelper.Status;
            Span<int> ceilingStart = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            Span<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);
            Span<int> portalFrom = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalFrom);
            Span<int> portalTo = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalTo);

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            for (int x = wallFromX; x <= wallToX; x++)
            {
                RenderColumnStatus columnStatus = RenderWindowHelper.Status[x];

                if (!columnStatus.WallRenderable)
                {
                    status[x] = RenderColumnStatus.FinishedRendering;
                    continue;
                }

                int floorEndY = floorEnd[x];
                int ceilingStartY = ceilingStart[x];


                int portalFromY = portalFrom[x];
                int portalToY = portalTo[x]; 
                int portalFromYClamped = Math.Clamp(portalFromY, ceilingStartY, floorEndY);
                int portalToYClamped = Math.Clamp(portalToY, ceilingStartY, floorEndY);

                ceilingStart[x] = portalFromYClamped;
                floorEnd[x] = portalToYClamped;
                status[x] ^= RenderColumnStatus.CanRenderWall;
            }

            // if sector height matches top or bottom offset only top or bottom texture was drawn
            // no middle texture is possible, thus we can treat this as basic wall
            return !(floorOffset == sectorHeight || sectorHeight == -ceilOffset);
        }


        private void DrawUpperSkyboxPortalWall(
            PortalPlayerSnapshot player,
            ReadOnlySpan<Sector> sectors,
            RenderablePortalWall renderableWall)
        {
            (int sectorHeight, int ceilOffset, _) = CalculatePortalOffsets(sectors, renderableWall.Wall);

            float oneOverSectorHeight = 1f / sectorHeight;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            ReadOnlySpan<RenderColumnStatus> status = RenderWindowHelper.Status;
            ReadOnlySpan<int> ceilingStart = RenderWindowHelper.CeilingStart;
            ReadOnlySpan<int> wallStart = RenderWindowHelper.WallStart;
            ReadOnlySpan<int> wallEnd = RenderWindowHelper.WallEnd;
            ReadOnlySpan<int> floorEnd = RenderWindowHelper.FloorEnd;
            ReadOnlySpan<int> clampedFrom = memoryPool.GetBucket<int>(MemoryPoolBucket.ClampedFrom);

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int width = PixelWidth;

            RenderableWall wall = renderableWall.Wall;
            TextureInfo upperTexture = wall.UpperTexture!;
            ref BGRA upperTexturePtr = ref MemoryMarshal.GetReference(upperTexture.Texture.GetBinary(false, wall.Shade));
            ref uint upperTextureUintPtr = ref Unsafe.As<BGRA, uint>(ref upperTexturePtr);
            ref float angleCachePtr = ref memoryPool.GetBucketRef<float>(MemoryPoolBucket.AngleCache);

            for (int x = wallFromX; x <= wallToX; x++)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    continue;
                }

                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];
                int floorEndY = floorEnd[x];
                int ceilingStartY = ceilingStart[x];
                int fromYClamped = clampedFrom[x];

                float pixelsPerHeight = (wallEndY - wallStartY) * oneOverSectorHeight;


                // Portal Calculation
                int ceilPixelOffset = float.ConvertToIntegerNative<int>(pixelsPerHeight * ceilOffset);
                int portalFromY = wallStartY - ceilPixelOffset;
                int portalFromYClamped = Math.Clamp(portalFromY, ceilingStartY, floorEndY);

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
        }

        private void DrawLowerSkyboxPortalWall(
                PortalPlayerSnapshot player,
                ReadOnlySpan<Sector> sectors,
                RenderablePortalWall renderableWall)
        {
            (int sectorHeight, _, int floorOffset) = CalculatePortalOffsets(sectors, renderableWall.Wall);

            float oneOverSectorHeight = 1f / sectorHeight;

            ref uint screenPtr = ref GetScreenPtr<uint>();

            ReadOnlySpan<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            ReadOnlySpan<int> ceilingStart = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            ReadOnlySpan<int> wallStart = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStart);
            ReadOnlySpan<int> wallEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEnd);
            ReadOnlySpan<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);
            ReadOnlySpan<int> clampedTo = memoryPool.GetBucket<int>(MemoryPoolBucket.ClampedTo);

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int width = PixelWidth;

            RenderableWall wall = renderableWall.Wall;
            TextureInfo upperTexture = wall.UpperTexture!;
            ref BGRA upperTexturePtr = ref MemoryMarshal.GetReference(upperTexture.Texture.GetBinary(false, wall.Shade));
            ref uint upperTextureUintPtr = ref Unsafe.As<BGRA, uint>(ref upperTexturePtr);
            ref float angleCachePtr = ref memoryPool.GetBucketRef<float>(MemoryPoolBucket.AngleCache);

            for (int x = wallFromX; x <= wallToX; x++)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    continue;
                }

                int wallStartY = wallStart[x];
                int wallEndY = wallEnd[x];
                int floorEndY = floorEnd[x];
                int ceilingStartY = ceilingStart[x];
                int toYClamped = clampedTo[x];

                float pixelsPerHeight = (wallEndY - wallStartY) * oneOverSectorHeight;

                // Portal Calculation
                int floorPixelOffset = float.ConvertToIntegerNative<int>(pixelsPerHeight * floorOffset);
                int portalToY = wallEndY - floorPixelOffset;
                int portalToYClamped = Math.Clamp(portalToY, ceilingStartY, floorEndY);

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
        }

        private void DrawUpperPortalWall(
            ReadOnlySpan<Sector> sectors,
            RenderablePortalWall renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;
            TextureInfo upperTexture = wall.UpperTexture!;

            (_, _, bool upperFlipY) = GetFlags(upperTexture);

            ReadOnlySpan<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            Span<int> statusInt = memoryPool.GetBucket<int>(MemoryPoolBucket.RenderColumnStatus);
            Span<int> topTextureYLocation = memoryPool.GetBucket<int>(MemoryPoolBucket.TopTextureYLocation);
            Span<int> wallStart = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStart);
            Span<int> topTextureXLocation = memoryPool.GetBucket<int>(MemoryPoolBucket.TopTextureXLocation);
            Span<int> clampedFrom = memoryPool.GetBucket<int>(MemoryPoolBucket.ClampedFrom);
            Span<int> portalFrom = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalFrom);
            Span<int> portalFromClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalClamped);


            uint width = (uint)PixelWidth;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            ref uint upperTexturePtr = ref upperTexture.Texture.GetBinaryRef<uint>(true, wall.Shade);

            ref uint screenPtr = ref GetScreenPtr<uint>();

            using TempBuffer<uint> upperBuffer = TempBuffer<uint>.GetBuffer(upperTexture.Height);

            int upperTextureStart = upperTexture.YOffset << 16;
            int textureWidth = upperTexture.Height;

            int length = wallToX - wallFromX;

            if (Vector.IsHardwareAccelerated && length > Vector<int>.Count)
            {
                int rem = (wallToX - wallFromX) % Vector<int>.Count;
                wallToX -= rem;

                Vector<int> upperTextureStartV = Vector.Create(upperTextureStart);
                Vector<int> wallRenderableV = Vector.Create((int)(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderWall));

                for (int x = wallFromX; x < wallToX;)
                {
                    Vector<int> statusV = Vector.LoadUnsafe(ref statusInt[x]);
                    //
                    Vector<int> wallStartYV = Vector.LoadUnsafe(ref wallStart[x]);
                    Vector<int> portalFromYClampedV = Vector.LoadUnsafe(ref portalFromClamped[x]);
                    //
                    Vector<int> textureXPosV = Vector.LoadUnsafe(ref topTextureXLocation[x]);
                    Vector<int> fromYClampedV = Vector.LoadUnsafe(ref clampedFrom[x]);
                    Vector<int> textureYIncrV = Vector.LoadUnsafe(ref topTextureYLocation[x]);
                    //
                    Vector<int> textureYPosV = upperTextureStartV - textureYIncrV * (wallStartYV - fromYClampedV);

                    bool repeat = (statusV & wallRenderableV) == wallRenderableV && Vector.All(textureXPosV, textureXPosV[0]);

                    if (repeat)
                    {
                        textureYPosV = SharedHelpers.EnsureOffsetIsPositive(Vector.Create(textureWidth << 16), textureYPosV);
                        int textureYPos = textureXPosV[0];
                        CalculateAndCacheWallColumn(upperBuffer, ref upperTexturePtr, textureYPos, upperFlipY);

                        RenderMultipleWallLinesT(
                            width,
                            (uint)x,
                            textureWidth,
                            // Y
                            fromYClampedV.As<int, uint>(),
                            portalFromYClampedV.As<int, uint>(),
                            // Texture
                            textureYPosV.As<int, uint>(),
                            textureYIncrV.As<int, uint>(),
                            ref screenPtr,
                            ref upperBuffer.Pointer
                        );

                        x += Vector<int>.Count;
                        continue;
                    }


                    for (int i = 0; i < Vector<int>.Count; i++, x++)
                    {
                        RenderColumnStatus columnStatus = (RenderColumnStatus)statusV[i];

                        if (!columnStatus.WallRenderable)
                        {
                            continue;
                        }

                        int textureXPos = textureYPosV[i];
                        int textureYPos = textureXPosV[i];
                        int fromYClamped = fromYClampedV[i];
                        int portalFromYClamped = portalFromYClampedV[i];
                        int textureXIncr = textureYIncrV[i];

                        textureXPos = SharedHelpers.EnsureOffsetIsPositive(textureWidth << 16, textureXPos);

                        CalculateAndCacheWallColumn(upperBuffer, ref upperTexturePtr, textureYPos, upperFlipY);

                        RenderWallLine2(
                            width,
                            (uint)x,
                            textureWidth,
                            (uint)fromYClamped,
                            (uint)portalFromYClamped,
                            (uint)textureXPos,
                            (uint)textureXIncr,
                            ref screenPtr,
                            ref upperBuffer.Pointer
                        );
                    }
                }

                wallFromX = wallToX;
                wallToX += rem;
            }

            for (int x = wallFromX; x <= wallToX; x++)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    continue;
                }

                int wallStartY = wallStart[x];
                int fromYClamped = clampedFrom[x];
                int textureYPos = topTextureXLocation[x];
                int textureXIncr = topTextureYLocation[x];

                // Portal Calculation
                int portalFromY = portalFrom[x];
                int portalFromYClamped = portalFromClamped[x];

                // Calculate Upper  Texture Position
                int textureXPos = upperTextureStart - textureXIncr * (wallStartY - fromYClamped);

                textureXPos = SharedHelpers.EnsureOffsetIsPositive(textureWidth << 16, textureXPos);

                CalculateAndCacheWallColumn(upperBuffer, ref upperTexturePtr, textureYPos, upperFlipY);

                RenderWallLine2(
                    width,
                    (uint)x,
                    textureWidth,
                    (uint)fromYClamped,
                    (uint)portalFromYClamped,
                    (uint)textureXPos,
                    (uint)textureXIncr,
                    ref screenPtr,
                    ref upperBuffer.Pointer
                );
            }
        }

        private void DrawLowerPortalWall(
            ReadOnlySpan<Sector> sectors,
            RenderablePortalWall renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;
            TextureInfo lowerTexture = wall.LowerTexture!;

            (_, _, bool lowerFlipY) = GetFlags(lowerTexture);

            ReadOnlySpan<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            Span<int> statusInt = memoryPool.GetBucket<int>(MemoryPoolBucket.RenderColumnStatus);
            Span<int> bottomTextureYLocation = memoryPool.GetBucket<int>(MemoryPoolBucket.BottomTextureYLocation);
            Span<int> bottomTextureXLocation = memoryPool.GetBucket<int>(MemoryPoolBucket.BottomTextureXLocation);
            Span<int> portalTo = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalTo);
            Span<int> clampedTo = memoryPool.GetBucket<int>(MemoryPoolBucket.ClampedTo);
            Span<int> wallStart = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStart);
            Span<int> portalToClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalClamped);

            int width = PixelWidth;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            ref uint lowerTexturePtr = ref lowerTexture.Texture.GetBinaryRef<uint>(true, wall.Shade);

            ref uint screenPtr = ref GetScreenPtr<uint>();

            using TempBuffer<uint> lowerBuffer = TempBuffer<uint>.GetBuffer(lowerTexture.Height);

            int lowerTextureStart = lowerTexture.YOffset << 16;
            int textureWidth = lowerTexture.Height;

            lowerTextureStart = SharedHelpers.EnsureOffsetIsPositive(textureWidth << 8, lowerTextureStart >> 1);

            int rem = (wallToX - wallFromX) % Vector<int>.Count;
            wallToX -= rem;

            Vector<int> lowerTextureStartV = Vector.Create(lowerTextureStart);

            Vector<int> wallRenderableV = Vector.Create((int)(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderWall));

            for (int x = wallFromX; x < wallToX;)
            {
                Vector<int> statusV = Vector.LoadUnsafe(ref statusInt[x]);
                Vector<int> toYClampedV = Vector.LoadUnsafe(ref clampedTo[x]);
                Vector<int> textureXPosV = Vector.LoadUnsafe(ref bottomTextureXLocation[x]);
                Vector<int> textureXIncrV = Vector.LoadUnsafe(ref bottomTextureYLocation[x]);

                Vector<int> wallStartYV = Vector.LoadUnsafe(ref wallStart[x]);

                // Portal Calculation
                Vector<int> portalToYV = Vector.LoadUnsafe(ref portalTo[x]);
                Vector<int> portalToYClampedV = Vector.LoadUnsafe(ref portalToClamped[x]);
                Vector<int> textureYPosV = textureXIncrV * (portalToYClampedV - wallStartYV) + lowerTextureStartV;

                bool repeat = (statusV & wallRenderableV) == wallRenderableV && Vector.All(textureXPosV, textureXPosV[0]);

                if (repeat)
                {
                    textureYPosV = SharedHelpers.EnsureOffsetIsPositive(Vector.Create(textureWidth << 16), textureYPosV);
                    int textureYPos = textureXPosV[0];
                    CalculateAndCacheWallColumn(lowerBuffer, ref lowerTexturePtr, textureYPos, lowerFlipY);

                    RenderMultipleWallLinesT(
                        (uint)width,
                        (uint)x,
                        textureWidth,
                        portalToYClampedV.As<int, uint>(),
                        toYClampedV.As<int, uint>(),
                        textureYPosV.As<int, uint>(),
                        textureXIncrV.As<int, uint>(),
                        ref screenPtr,
                        ref lowerBuffer.Pointer
                    );

                    x += Vector<int>.Count;
                    continue;
                }

                for (int i = 0; i < Vector<int>.Count; i++, x++)
                {
                    RenderColumnStatus columnStatus = (RenderColumnStatus)statusV[i];

                    if (!columnStatus.WallRenderable)
                    {
                        continue;
                    }

                    int portalToYClamped = portalToYClampedV[i];
                    int toYClamped = toYClampedV[i];
                    int textureXPos = textureYPosV[i];
                    int textureXIncr = textureXIncrV[i];
                    int textureYPos = textureXPosV[i];

                    textureXPos = SharedHelpers.EnsureOffsetIsPositive(textureWidth << 16, textureXPos);

                    CalculateAndCacheWallColumn(lowerBuffer, ref lowerTexturePtr, textureYPos, lowerFlipY);

                    RenderWallLine2(
                        (uint)width,
                        (uint)x,
                        textureWidth,
                        (uint)portalToYClamped,
                        (uint)toYClamped,
                        (uint)textureXPos,
                        (uint)textureXIncr,
                        ref screenPtr,
                        ref lowerBuffer.Pointer
                    );
                }
            }

            wallFromX = wallToX;
            wallToX += rem;


            for (int x = wallFromX; x <= wallToX; x++)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.WallRenderable)
                {
                    continue;
                }

                int toYClamped = clampedTo[x];
                int portalToY = portalTo[x];
                int textureYPos = bottomTextureXLocation[x];
                int textureXIncr = bottomTextureYLocation[x];
                int wallStartX = wallStart[x];

                int portalToYClamped = portalToClamped[x];
                int textureXPos = textureXIncr * (portalToYClamped - wallStartX) + lowerTextureStart;
                textureXPos = SharedHelpers.EnsureOffsetIsPositive(textureWidth << 16, textureXPos);

                CalculateAndCacheWallColumn(lowerBuffer, ref lowerTexturePtr, textureYPos, lowerFlipY);

                RenderWallLine2(
                    (uint)width,
                    (uint)x,
                    textureWidth,
                    (uint)portalToYClamped,
                    (uint)toYClamped,
                    (uint)textureXPos,
                    (uint)textureXIncr,
                    ref screenPtr,
                    ref lowerBuffer.Pointer
                );
            }
        }

        private void PrecalculateUpperWallDistance(
            Sector sector,
            RenderablePortalWall renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;

            Span<int> xLocation = memoryPool.GetBucket<int>(MemoryPoolBucket.TopTextureXLocation);
            Span<int> yLocation = memoryPool.GetBucket<int>(MemoryPoolBucket.TopTextureYLocation);
            Span<int> portalFrom = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalFrom);

            PrecalculateWallDistanceShared(renderableWall, sector, wall.UpperTexture!, xLocation, yLocation, portalFrom);
        }

        private void PrecalculateLowerWallDistance(
            Sector sector,
            RenderablePortalWall renderableWall)
        {
            RenderableWall wall = renderableWall.Wall;

            Span<int> xLocation = memoryPool.GetBucket<int>(MemoryPoolBucket.BottomTextureXLocation);
            Span<int> yLocation = memoryPool.GetBucket<int>(MemoryPoolBucket.BottomTextureYLocation);
            Span<int> portalTo = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalTo);


            PrecalculateWallDistanceShared(renderableWall, sector, wall.LowerTexture!, xLocation, yLocation, portalTo);
        }

        private void PrecalculateWallDistanceShared(
            RenderablePortalWall renderableWall, Sector sector, TextureInfo textureInfo,
            scoped Span<int> xLocation, scoped Span<int> yLocation, scoped Span<int> portal)
        {
            Span<float> distance = memoryPool.GetBucket<float>(MemoryPoolBucket.Distance);
            Span<int> wallStart = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStart);
            Span<int> wallEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEnd);
            Span<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            Span<int> ceilingStart = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            Span<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);
            Span<int> clampedFrom = memoryPool.GetBucket<int>(MemoryPoolBucket.ClampedFrom);
            Span<int> clampedTo = memoryPool.GetBucket<int>(MemoryPoolBucket.ClampedTo);

            Span<int> portalClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalClamped);

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

                    Vector<int> ceilingStartYV = Vector.LoadUnsafe(ref ceilingStart[x]);
                    Vector<int> floorEndYV = Vector.LoadUnsafe(ref floorEnd[x]);
                    Vector<int> portalV = Vector.LoadUnsafe(ref portal[x]);

                    Vector<int> clampedFromYV = Vector.ClampNative(wallStartV, ceilingStartYV, floorEndYV);
                    Vector<int> clampedToYV = Vector.ClampNative(wallEndV, ceilingStartYV, floorEndYV);
                    Vector<int> clampedPortal = Vector.ClampNative(portalV, ceilingStartYV, floorEndYV);

                    Vector.StoreUnsafe(clampedFromYV, ref clampedFrom[x]);
                    Vector.StoreUnsafe(clampedToYV, ref clampedTo[x]);
                    Vector.StoreUnsafe(clampedPortal, ref portalClamped[x]);

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

                int ceilingStartY = ceilingStart[x];
                int floorEndY = floorEnd[x];
                int portalY = portal[x];

                int clamptedFromY = Math.Clamp(wallStartY, ceilingStartY, floorEndY);
                int clamptedToY = Math.Clamp(wallEndY, ceilingStartY, floorEndY);
                int clamptedPortalY = Math.Clamp(portalY, ceilingStartY, floorEndY);

                clampedFrom[x] = clamptedFromY;
                clampedTo[x] = clamptedToY;
                portalClamped[x] = clamptedPortalY;
            }
        }

    }
}
