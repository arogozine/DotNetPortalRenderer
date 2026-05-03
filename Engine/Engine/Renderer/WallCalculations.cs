using RenderingEngine.Models;
using RenderingEngine.Tooling;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private void CalculateUpperTextureYIncrement(RenderablePortalWall portalWall, TextureInfo textureInfo)
        {
            RenderableWall wall = portalWall.Wall;

            int textureStart = textureInfo.YOffset << 16;

            Span<int> textureYIncrement = memoryPool.GetBucket<int>(MemoryPoolBucket.TextureYIncrement);
            Span<int> startingYTexturePosition = memoryPool.GetBucket<int>(MemoryPoolBucket.StartingYTexturePosition);
            Span<int> ceil = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            Span<int> wallStartSloped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped);

            (int textureHeight, _, _, float scaledTextureHeight) = CalculateScale(wall.Sector, wall, textureInfo);

            int offset = portalWall.Offset;
            int wallFromX = portalWall.XLeft;
            int wallToX = portalWall.XRight;

            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(wall, offset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            for (int x = wallFromX; x <= wallToX; x++)
            {
                float textureYIncr = scaledTextureHeight / (wallEndY - wallStartY);

                int ceilingY = ceil[x];
                int wallSlopedStartY = wallStartSloped[x];

                Debug.Assert(ceilingY >= 0);

                float topOffset = 0f;

                // ceiling (render start) is lower than sloped wall start
                // increment texture start to accomodate
                if (ceilingY > wallSlopedStartY)
                {
                    topOffset += ceilingY - wallSlopedStartY;
                }

                // slope hides part of the wall,
                // increment texture start to accomodate
                topOffset += wallSlopedStartY - wallStartY;

                int textureYPosY = float.ConvertToIntegerNative<int>(textureStart + topOffset * textureYIncr);
                textureYPosY = SharedHelpers.EnsureOffsetIsPositive(textureHeight << 16, textureYPosY);

                Debug.Assert(textureYPosY <= textureHeight << 16);

                startingYTexturePosition[x] = textureYPosY;
                textureYIncrement[x] = float.ConvertToIntegerNative<int>(textureYIncr);

                wallStartY += ceilDistIncr;
                wallEndY += floorDistIncr;
            }
        }

        private void CalculateLowerTextureYIncrement(RenderablePortalWall portalWall, TextureInfo textureInfo)
        {
            RenderableWall wall = portalWall.Wall;

            int textureStart = textureInfo.YOffset << 16;

            Span<int> portalTo = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalTo);
            Span<int> portalToClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalToClamped);
            Span<int> startingYTexturePosition = memoryPool.GetBucket<int>(MemoryPoolBucket.StartingYTexturePosition);
            Span<int> ceil = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);

            Span<int> textureYIncrement = memoryPool.GetBucket<int>(MemoryPoolBucket.TextureYIncrement);

            (int textureHeight, _, _, float scaledTextureHeight) = CalculateScale(wall.Sector, wall, textureInfo);

            int offset = portalWall.Offset;
            int wallFromX = portalWall.XLeft;
            int wallToX = portalWall.XRight;

            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(wall, offset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            for (int x = wallFromX; x <= wallToX; x++)
            {
                float textureYIncr = scaledTextureHeight / (wallEndY - wallStartY);

                int portalToY = portalTo[x];
                int portalToSlopedY = portalToClamped[x];
                int ceilY = ceil[x];

                float topOffset = 0;

                if (portalToSlopedY < ceilY)
                {
                    topOffset -= portalToSlopedY - ceilY;
                }

                topOffset += portalToSlopedY - portalToY;
                topOffset *= textureYIncr;

                int textureYPosY = float.ConvertToIntegerNative<int>(textureStart + topOffset);

                textureYPosY = SharedHelpers.EnsureOffsetIsPositive(textureHeight << 16, textureYPosY);

                Debug.Assert(textureYPosY <= textureHeight << 16);

                startingYTexturePosition[x] = textureYPosY;
                textureYIncrement[x] = float.ConvertToIntegerNative<int>(textureYIncr);

                wallStartY += ceilDistIncr;
                wallEndY += floorDistIncr;
            }
        }

        private void CalculateDistance(RenderablePortalWall renderableWall)
        {
            Span<float> distance = memoryPool.GetBucket<float>(MemoryPoolBucket.Distance);

            RenderableWall wall = renderableWall.Wall;

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, PixelWidth, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                float fromToYdist = MathFormulas.CalculateDistance2(cameraRay, t1, d2y, d2x);

                distance[x] = fromToYdist;
            }
        }

        private void CalculateTextureDistanceAndXPosition(RenderablePortalWall renderableWall, TextureInfo textureInfo)
        {
            Span<int> xLocation = memoryPool.GetBucket<int>(MemoryPoolBucket.TextureXLocation);

            int width = PixelWidth;

            Span<float> distance = memoryPool.GetBucket<float>(MemoryPoolBucket.Distance);

            RenderableWall wall = renderableWall.Wall;

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;


            int textureHeight = textureInfo.Height;
            int textureWidth = textureInfo.Width;
            float xOffset = textureInfo.XOffset;

            if (textureInfo.XScale is float xScale)
            {
                float wallLength = wall.Length;
                xScale = xScale / wallLength * textureWidth;
            }
            else
            {
                xScale = 1f;
            }

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);

            bool flipX = textureInfo.RenderingOptions.IsFlippedX;
            flipX = wall.Flipped ? !flipX : flipX;
            float rX = flipX ? wall.R2.X : wall.R1.X;
            float rY = flipX ? wall.R2.Y : wall.R1.Y;


            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                (float fromToXdist, float fromToYdist) = MathFormulas.CalculateRayIntersection(cameraRay, t1, d2y, d2x);

                float distX = rX - fromToXdist;
                float distY = rY - fromToYdist;

                float textureDist = MathF.Sqrt(distX * distX + distY * distY);

                xLocation[x] = float.ConvertToIntegerNative<int>(MathF.FusedMultiplyAdd(textureDist, xScale, xOffset));
                xLocation[x] = (xLocation[x] % textureWidth) * textureHeight;

                distance[x] = fromToYdist;
            }
        }

        private void CalculatePortalClamp(RenderablePortalWall renderableWall)
        {
            Span<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);

            Span<int> wallStartClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped);
            Span<int> wallEndClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped);

            Span<int> portalFromClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalFromClamped);
            Span<int> portalToClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalToClamped);

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            for (int x = wallFromX; x <= wallToX; x++)
            {
                if (status[x].IsFinished)
                {
                    continue;
                }

                int wallStartClampedY = wallStartClamped[x];
                int wallEndClampedY = wallEndClamped[x];

                int portalFromClampedY = portalFromClamped[x];
                int portalToClampedY = portalToClamped[x];

                portalFromClampedY = Math.Clamp(portalFromClampedY, wallStartClampedY, wallEndClampedY);
                portalToClampedY = Math.Clamp(portalToClampedY, wallStartClampedY, wallEndClampedY);

                portalFromClamped[x] = portalFromClampedY;
                portalToClamped[x] = portalToClampedY;
            }
        }

        private void CalculateWallClamp(RenderablePortalWall renderableWall)
        {
            Span<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);

            Span<int> ceilingStart = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            Span<int> wallStartClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped);
            Span<int> wallEndClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped);
            Span<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;


            for (int x = wallFromX; x <= wallToX; x++)
            {
                int ceilingStartY = ceilingStart[x];
                int floorEndY = floorEnd[x];

                int wallStartY = wallStartClamped[x];
                int wallEndY = wallEndClamped[x];

                int clamptedFromY = Math.Clamp(wallStartY, ceilingStartY, floorEndY);
                int clamptedToY = Math.Clamp(wallEndY, ceilingStartY, floorEndY);

                Debug.Assert(ceilingStartY >= 0);
                Debug.Assert(floorEndY >= 0);
                wallStartClamped[x] = clamptedFromY;
                wallEndClamped[x] = clamptedToY;

                if (!status[x].WallRenderable || clamptedFromY >= clamptedToY)
                {
                    status[x] = RenderColumnStatus.FinishedRendering;
                }
            }
        }

        private void CalculateNewFloorCeiling(int from, int to)
        {
            Span<int> portalFromClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalFromClamped);
            Span<int> portalToClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalToClamped);

            Span<int> ceilingStart = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            Span<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);

            Span<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);

            for (int x = from; x <= to; x++)
            {
                if (!status[x].WallRenderable)
                {
                    status[x] = RenderColumnStatus.FinishedRendering;
                    continue;
                }

                ceilingStart[x] = portalFromClamped[x];
                floorEnd[x] = portalToClamped[x];
                status[x] ^= RenderColumnStatus.CanRenderWall;

            }
        }

        public RenderColumnStatus NewDepth()
        {
            Span<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            ReadOnlySpan<int> ceilingStart = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            ReadOnlySpan<int> wallStart = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped);
            ReadOnlySpan<int> wallEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped);
            ReadOnlySpan<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);

            RenderColumnStatus renderColumnStatus = default;

            for (int i = 0; i < status.Length; i++)
            {
                RenderColumnStatus columnStatus = status[i];

                if (columnStatus.IsFinished)
                {
                    continue;
                }
                else if (columnStatus.IsCalculated)
                {
                    columnStatus = RecalculateRenderWindow(i, false, status, ceilingStart, floorEnd, wallStart, wallEnd);
                }
                else
                {
                    columnStatus = RenderColumnStatus.FinishedRendering;
                    status[i] = RenderColumnStatus.FinishedRendering;
                }

                renderColumnStatus |= columnStatus;
            }

            // this allows us to know what, if anything, we can still render
            return renderColumnStatus;
        }

        public static RenderColumnStatus RecalculateRenderWindow(
            int x,
            bool calculated,
            scoped Span<RenderColumnStatus> status,
            scoped ReadOnlySpan<int> ceilingStart,
            scoped ReadOnlySpan<int> floorEnd,
            scoped ReadOnlySpan<int> wallStart,
            scoped ReadOnlySpan<int> wallEnd
            )
        {
            RenderColumnStatus statusY;
            int ceilingStartY = ceilingStart[x];
            int floorEndY = floorEnd[x];
            int wallStartY = wallStart[x];
            int wallEndY = wallEnd[x];

            bool windowExists = ceilingStartY < floorEndY;

            bool canRenderCeiling = windowExists && ceilingStartY < wallStartY && ceilingStartY < floorEndY;
            bool canRenderFloor = windowExists && wallEndY < floorEndY;
            bool canRenderWall = windowExists && wallStartY < wallEndY && ceilingStartY < floorEndY;
            bool canRenderPortal = windowExists && floorEndY < ceilingStartY && wallStartY < floorEndY;

            RenderColumnStatus startingStatus = calculated ? RenderColumnStatus.Calculated : default;

            if (!windowExists || !(canRenderCeiling || canRenderFloor || canRenderWall || canRenderPortal))
            {
                statusY = RenderColumnStatus.FinishedRendering;
            }
            else
            {
                statusY = startingStatus;

                if (canRenderCeiling)
                {
                    statusY |= RenderColumnStatus.CanRenderCeiling;
                }

                if (canRenderFloor)
                {
                    statusY |= RenderColumnStatus.CanRenderFloor;
                }

                if (canRenderWall)
                {
                    statusY |= RenderColumnStatus.CanRenderWall;
                }

                if (canRenderPortal)
                {
                    statusY |= RenderColumnStatus.CanRenderPortal;
                }
            }

            status[x] = statusY;
            return statusY;
        }

    }
}
