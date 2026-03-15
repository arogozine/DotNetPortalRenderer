using RenderingEngine.Models;
using RenderingEngine.Tooling;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private void CalculateTextureYIncrement(RenderablePortalWall portalWall, TextureInfo textureInfo)
        {
            RenderableWall wall = portalWall.Wall;

            int textureStart = textureInfo.YOffset << 16;

            Span<int> portalTo = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalTo);
            Span<int> portalToClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalToClamped);
            Span<int> startingYTexturePosition = memoryPool.GetBucket<int>(MemoryPoolBucket.StartingYTexturePosition);

            Span<int> textureYIncrement = memoryPool.GetBucket<int>(MemoryPoolBucket.TextureYIncrement);

            (_, _, _, float scaledTextureWidth) = CalculateScale(wall.Sector, wall, textureInfo);

            int offset = portalWall.Offset;
            int wallFromX = portalWall.XLeft;
            int wallToX = portalWall.XRight;

            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(wall, offset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            (int textureHeight, _, _, _) = CalculateScale(portalWall.Wall.Sector, wall, textureInfo);

            for (int x = wallFromX; x <= wallToX; x++)
            {
                float textureYIncr = scaledTextureWidth / (wallEndY - wallStartY);

                int portalToY = portalTo[x];
                int portalToSlopedY = portalToClamped[x];
                int offsetY = portalToSlopedY - portalToY;

                float topOffset = portalToSlopedY < 0 ? -portalToSlopedY * textureYIncr : 0f;

                int textureYPosY = float.ConvertToIntegerNative<int>(textureStart + (offsetY) * textureYIncr + topOffset);

                textureYPosY = SharedHelpers.EnsureOffsetIsPositive(textureHeight << 16, textureYPosY);

                startingYTexturePosition[x] = textureYPosY;
                textureYIncrement[x] = float.ConvertToIntegerNative<int>(textureYIncr);

                wallStartY += ceilDistIncr;
                wallEndY += floorDistIncr;
            }
        }

        private void CalculateTextureDistanceAndXPosition(Span<int> xLocation, RenderablePortalWall renderableWall, TextureInfo textureInfo)
        {
            int width = PixelWidth;

            Span<float> distance = memoryPool.GetBucket<float>(MemoryPoolBucket.Distance);

            RenderableWall wall = renderableWall.Wall;

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int length = (wallToX - wallFromX);


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

            bool textureWidthEven = SharedHelpers.IsPowerOfTwo(textureWidth);
            int widthMask = textureWidthEven ? textureWidth - 1 : default;

            if (Vector.IsHardwareAccelerated && length > Vector<float>.Count)
            {
                int rem = length % Vector<float>.Count;
                wallToX -= rem;

                Vector<int> widthMaskV = Vector.Create(widthMask);
                Vector<int> textureHeightV = Vector.Create(textureHeight);

                Vector<float> t1V = Vector.Create(t1);
                Vector<float> d2yV = Vector.Create(d2y);
                Vector<float> d2xV = Vector.Create(d2x);
                Vector<float> rXV = Vector.Create(rX);
                Vector<float> rYV = Vector.Create(rY);
                Vector<float> xScaleV = Vector.Create(xScale);
                Vector<float> xOffsetV = Vector.Create(xOffset);

                Vector<float> cameraRayV = Vector.CreateSequence(cameraRay, cameraWidthIncr);
                Vector<float> cameraWidthIncrV = Vector.Create(cameraWidthIncr * Vector<float>.Count);

                for (int x = wallFromX; x < wallToX; x += Vector<float>.Count, cameraRayV += cameraWidthIncrV)
                {
                    (Vector<float> fromToXdist, Vector<float> fromToYdist) = MathFormulas.CalculateRayIntersection(cameraRayV, t1V, d2yV, d2xV);
                    Vector<float> distX = rXV - fromToXdist;
                    Vector<float> distY = rYV - fromToYdist;

                    Vector<float> textureDist = Vector.SquareRoot(distX * distX + distY * distY);
                    Vector<int> topXLocationV = Vector.ConvertToInt32Native(Vector.FusedMultiplyAdd(textureDist, xScaleV, xOffsetV));

                    if (textureWidthEven)
                    {
                        topXLocationV = (topXLocationV & widthMaskV) * textureHeightV;
                        Vector.StoreUnsafe(topXLocationV, ref xLocation[x]);
                    }
                    else
                    {
                        for (int i = 0; i < Vector<float>.Count; i++)
                        {
                            xLocation[x + i] = (topXLocationV[i] % textureWidth) * textureHeight;
                        }
                    }

                    Vector.StoreUnsafe(fromToYdist, ref distance[x]);
                }

                wallFromX = wallToX;
                wallToX += rem;
                cameraRay = cameraRayV[0];
            }

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

        private void CalculateTextureYForBasicWall(RenderablePortalWall renderableWall, TextureInfo textureInfo)
        {
            Span<int> wallStart = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped);
            Span<int> textureYIncrement = memoryPool.GetBucket<int>(MemoryPoolBucket.TextureYIncrement);
            Span<int> startingYTexturePosition = memoryPool.GetBucket<int>(MemoryPoolBucket.StartingYTexturePosition);
            Span<int> wallStartClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStart);

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int length = wallToX - wallFromX;

            var wall = renderableWall.Wall;
            int textureStart = textureInfo.YOffset << 16;

            (int textureHeight, _, _, _) = CalculateScale(renderableWall.Wall.Sector, wall, textureInfo);

            textureStart = SharedHelpers.EnsureOffsetIsPositive(textureHeight << 16, textureStart);

            if (Vector.IsHardwareAccelerated && length > Vector<float>.Count)
            {
                int rem = length % Vector<float>.Count;
                wallToX -= rem;

                Vector<int> textureStartV = Vector.Create(textureStart);
                Vector<int> textureWidthV = Vector.Create(textureHeight << 16);

                for (int x = wallFromX; x < wallToX; x += Vector<float>.Count)
                {
                    Vector<int> wallStartV = Vector.LoadUnsafe(ref wallStart[x]);
                    Vector<int> wallStartClampedV = Vector.LoadUnsafe(ref wallStartClamped[x]);

                    Vector<int> textureYIncr = Vector.LoadUnsafe(ref textureYIncrement[x]);
                    Vector<int> textureYPosV = textureStartV + textureYIncr * (wallStartClampedV - wallStartV);
                    textureYPosV = SharedHelpers.EnsureOffsetIsPositive(textureWidthV, textureYPosV);

                    Vector.StoreUnsafe(textureYPosV, ref startingYTexturePosition[x]);
                }

                wallFromX = wallToX;
                wallToX += rem;
            }

            for (int x = wallFromX; x <= wallToX; x++)
            {
                int wallStartY = wallStart[x];
                int clamptedFromY = wallStartClamped[x];
                int textureYIncr = textureYIncrement[x];

                int textureYPosY = textureStart + textureYIncr * (clamptedFromY - wallStartY);
                textureYPosY = SharedHelpers.EnsureOffsetIsPositive(textureHeight << 16, textureYPosY);

                startingYTexturePosition[x] = textureYPosY;
            }
        }

        private void CalculateTextureYStartAndIncrement(bool fromTop, RenderablePortalWall renderableWall, TextureInfo textureInfo)
        {
            if (fromTop)
            {
                CalculateTextureYForBasicWall(renderableWall, textureInfo);
                return;
            }

            // CalculateLowerYForBasicWall(renderableWall, textureInfo);
            return;
        }


        private void CalculatePortalClampSloped(RenderablePortalWall renderableWall)
        {
            Span<int> wallStartClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStart);
            Span<int> wallEndClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEnd);

            Span<int> portalFromClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalFromClamped);
            Span<int> portalToClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalToClamped);

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int length = (wallToX - wallFromX);

            if (Vector.IsHardwareAccelerated && length > Vector<float>.Count)
            {
                int rem = length % Vector<float>.Count;
                wallToX -= rem;

                for (int x = wallFromX; x < wallToX; x += Vector<float>.Count)
                {
                    Vector<int> wallStartClampedV = Vector.LoadUnsafe(ref wallStartClamped[x]);
                    Vector<int> wallEndClampedV = Vector.LoadUnsafe(ref wallEndClamped[x]);

                    Vector<int> portalFromClampedV = Vector.LoadUnsafe(ref portalFromClamped[x]);
                    Vector<int> portalToClampedV = Vector.LoadUnsafe(ref portalToClamped[x]);

                    portalFromClampedV = Vector.ClampNative(portalFromClampedV, wallStartClampedV, wallEndClampedV);
                    portalToClampedV = Vector.ClampNative(portalToClampedV, wallStartClampedV, wallEndClampedV);

                    Vector.StoreUnsafe(portalFromClampedV, ref portalFromClamped[x]);
                    Vector.StoreUnsafe(portalToClampedV, ref portalToClamped[x]);
                }

                wallFromX = wallToX;
                wallToX += rem;
            }

            for (int x = wallFromX; x <= wallToX; x++)
            {
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

        private void CalculatePortalClamp(RenderablePortalWall renderableWall, bool wallSloped)
        {
            if (wallSloped)
            {
                CalculatePortalClampSloped(renderableWall);
                return;
            }

            Span<int> wallStartClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStart);
            Span<int> wallEndClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEnd);

            Span<int> portalFrom = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalFrom);
            Span<int> portalTo = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalTo);

            Span<int> portalFromClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalFromClamped);
            Span<int> portalToClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.PortalToClamped);

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int length = (wallToX - wallFromX);

            if (Vector.IsHardwareAccelerated && length > Vector<float>.Count)
            {
                int rem = length % Vector<float>.Count;
                wallToX -= rem;

                for (int x = wallFromX; x < wallToX; x += Vector<float>.Count)
                {
                    Vector<int> wallStartClampedV = Vector.LoadUnsafe(ref wallStartClamped[x]);
                    Vector<int> wallEndClampedV = Vector.LoadUnsafe(ref wallEndClamped[x]);

                    Vector<int> portalFromV = Vector.LoadUnsafe(ref portalFrom[x]);
                    Vector<int> portalToV = Vector.LoadUnsafe(ref portalTo[x]);

                    Vector<int> clamptedFromYV = Vector.ClampNative(portalFromV, wallStartClampedV, wallEndClampedV);
                    Vector<int> clamptedToYV = Vector.ClampNative(portalToV, wallStartClampedV, wallEndClampedV);

                    Vector.StoreUnsafe(clamptedFromYV, ref portalFromClamped[x]);
                    Vector.StoreUnsafe(clamptedToYV, ref portalToClamped[x]);
                }

                wallFromX = wallToX;
                wallToX += rem;
            }

            for (int x = wallFromX; x <= wallToX; x++)
            {
                int wallStartClampedY = wallStartClamped[x];
                int wallEndClampedY = wallEndClamped[x];

                int portalFromY = portalFrom[x];
                int portalToY = portalTo[x];

                int clamptedFromY = Math.Clamp(portalFromY, wallStartClampedY, wallEndClampedY);
                int clamptedToY = Math.Clamp(portalToY, wallStartClampedY, wallEndClampedY);

                portalFromClamped[x] = clamptedFromY;
                portalToClamped[x] = clamptedToY;
            }
        }

        private void CalculateWallClamp(RenderablePortalWall renderableWall)
        {
            Span<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);

            Span<int> ceilingStart = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            Span<int> wallStartClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped);
            Span<int> wallEndClamped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped);
            Span<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);

            Span<int> wallStart = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStart);
            Span<int> wallEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEnd);

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int length = (wallToX - wallFromX);

            if (Vector.IsHardwareAccelerated && length > Vector<float>.Count)
            {
                int rem = length % Vector<float>.Count;
                wallToX -= rem;

                for (int x = wallFromX; x < wallToX; x += Vector<float>.Count)
                {
                    Vector<int> ceilingStartYV = Vector.LoadUnsafe(ref ceilingStart[x]);
                    Vector<int> floorEndYV = Vector.LoadUnsafe(ref floorEnd[x]);

                    Vector<int> wallStartV = Vector.LoadUnsafe(ref wallStartClamped[x]);
                    Vector<int> wallEndV = Vector.LoadUnsafe(ref wallEndClamped[x]);

                    Vector<int> clamptedFromYV = Vector.ClampNative(wallStartV, ceilingStartYV, floorEndYV);
                    Vector<int> clamptedToYV = Vector.ClampNative(wallEndV, ceilingStartYV, floorEndYV);

                    Vector.StoreUnsafe(clamptedFromYV, ref wallStart[x]);
                    Vector.StoreUnsafe(clamptedToYV, ref wallEnd[x]);

                    // if after clamping there is nothing that can be rendered,
                    // set those columns as finished
                    for (int i = 0; i < Vector<int>.Count; i++)
                    {
                        int clamptedFromY = clamptedFromYV[i];
                        int clamptedToY = clamptedToYV[i];

                        if (!status[x + i].WallRenderable || clamptedFromY >= clamptedToY)
                        {
                            status[x + i] = RenderColumnStatus.FinishedRendering;
                        }
                    }
                }

                wallFromX = wallToX;
                wallToX += rem;
            }

            for (int x = wallFromX; x <= wallToX; x++)
            {
                int ceilingStartY = ceilingStart[x];
                int floorEndY = floorEnd[x];

                int wallStartY = wallStartClamped[x];
                int wallEndY = wallEndClamped[x];

                int clamptedFromY = Math.Clamp(wallStartY, ceilingStartY, floorEndY);
                int clamptedToY = Math.Clamp(wallEndY, ceilingStartY, floorEndY);

                wallStart[x] = clamptedFromY;
                wallEnd[x] = clamptedToY;

                if (!status[x].WallRenderable || clamptedFromY >= clamptedToY)
                {
                    status[x] = RenderColumnStatus.FinishedRendering;
                }
            }
        }
    }
}
