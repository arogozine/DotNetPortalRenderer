using RenderingEngine.Tooling;
using SoftwareRendererModels;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal unsafe abstract partial class PortalRenderer
    {
        private void CalculateUpperTextureYIncrement(RenderablePortalWall portalWall, GameTextureInfo textureInfo)
        {
            RenderableWall wall = portalWall.Wall;

            float textureStart = textureInfo.YOffset << 16;

            int* textureYIncrement = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.TextureYIncrement);
            int* startingYTexturePosition = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.StartingYTexturePosition);
            int* ceil = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.CeilingStart);
            int* wallStartSloped = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.WallStartClamped);

            (int textureHeight, float scaledTextureHeight) = CalculateScale(wall.Sector, textureInfo);
            int textureHeightShifted = textureHeight << 16;

            int offset = portalWall.Offset;
            int wallFromX = portalWall.XLeft;
            int wallToX = portalWall.XRight;

            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(wall, offset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            int length = wallToX - wallFromX;

            // AI Assisted
            if (Vector.IsHardwareAccelerated && length > Vector<float>.Count)
            {
                int rem = length & (Vector<float>.Count - 1);
                wallToX -= rem;

                Vector<float> ceilDistIncrV = Vector.Create(ceilDistIncr * Vector<float>.Count);
                Vector<float> floorDistIncrV = Vector.Create(floorDistIncr * Vector<float>.Count);
                Vector<float> wallStartYv = Vector.CreateSequence(wallStartY, ceilDistIncr);
                Vector<float> wallEndYv = Vector.CreateSequence(wallEndY, floorDistIncr);

                Vector<float> scaledTextureHeightV = Vector.Create(scaledTextureHeight);
                Vector<float> textureStartV = Vector.Create(textureStart);

                for (int x = wallFromX; x < wallToX; x += Vector<float>.Count)
                {
                    Vector<int> ceilingYv = Vector.Load(ceil + x);
                    Vector<int> wallSlopedStartYv = Vector.Load(wallStartSloped + x);

                    // ceiling (render start) is lower than sloped wall start
                    // increment texture start to accomodate
                    Vector<int> ceilDiffV = ceilingYv - wallSlopedStartYv;
                    Vector<float> topOffsetV = Vector.Max(Vector.ConvertToSingle(ceilDiffV), Vector<float>.Zero);

                    // slope hides part of the wall,
                    // increment texture start to accomodate
                    topOffsetV += Vector.ConvertToSingle(wallSlopedStartYv) - wallStartYv;

                    Vector<float> textureYIncrV = scaledTextureHeightV / (wallEndYv - wallStartYv);
                    Vector<float> textureYPosYv = Vector.FusedMultiplyAdd(topOffsetV, textureYIncrV, textureStartV);
                    Vector<int> textureYPosYIntV = Vector.ConvertToInt32Native(textureYPosYv);

                    for (int i = 0; i < Vector<float>.Count; i++)
                    {
                        startingYTexturePosition[x + i] = SharedHelpers.EnsureOffsetIsPositive(textureHeightShifted, textureYPosYIntV[i]);
                    }

                    Vector.Store(Vector.ConvertToInt32Native(textureYIncrV), textureYIncrement + x);

                    wallStartYv += ceilDistIncrV;
                    wallEndYv += floorDistIncrV;
                }

                wallStartY = wallStartYv[0];
                wallEndY = wallEndYv[0];

                wallFromX = wallToX;
                wallToX += rem;
            }

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
                textureYPosY = SharedHelpers.EnsureOffsetIsPositive(textureHeightShifted, textureYPosY);

                startingYTexturePosition[x] = textureYPosY;
                textureYIncrement[x] = float.ConvertToIntegerNative<int>(textureYIncr);

                wallStartY += ceilDistIncr;
                wallEndY += floorDistIncr;
            }
        }

        private void CalculateLowerTextureYIncrement(RenderablePortalWall portalWall, GameTextureInfo textureInfo, ReadOnlySpan<RenderableSector> sectors)
        {
            if (textureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.FromLower))
            {
                CalculateLowerTextureYIncrementForSwappedWalls(portalWall, textureInfo, sectors);
                return;
            }

            RenderableWall wall = portalWall.Wall;

            int textureStart = textureInfo.YOffset << 16;

            int* portalTo = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalTo);
            int* portalToClamped = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalToClamped);
            int* startingYTexturePosition = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.StartingYTexturePosition);
            int* ceil = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.CeilingStart);
            int* textureYIncrement = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.TextureYIncrement);

            (int textureHeight, float scaledTextureHeight) = CalculateScale(wall.Sector, textureInfo);
            int textureHeightShifted = textureHeight << 16;

            int offset = portalWall.Offset;
            int wallFromX = portalWall.XLeft;
            int wallToX = portalWall.XRight;

            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(wall, offset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            CalculateLowerTextureYIncrementCore(
                portalTo, portalToClamped, ceil, startingYTexturePosition, textureYIncrement,
                wallFromX, wallToX, textureStart, textureHeightShifted, scaledTextureHeight,
                wallStartY, ceilDistIncr, wallEndY, floorDistIncr);
        }

        private void CalculateLowerTextureYIncrementForSwappedWalls(RenderablePortalWall portalWall, GameTextureInfo textureInfo, ReadOnlySpan<RenderableSector> sectors)
        {
            RenderableWall wall = portalWall.Wall;

            int textureStart = textureInfo.YOffset << 16;

            int* portalTo = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalTo);
            int* portalToClamped = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalToClamped);
            int* startingYTexturePosition = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.StartingYTexturePosition);
            int* ceil = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.CeilingStart);
            int* textureYIncrement = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.TextureYIncrement);

            RenderableSector neighborSector = sectors[wall.Neighbor!.Value];
            (float floorZ_a, float _) = MathFormulas.CalculateZAtPoint(neighborSector, portalWall.Wall.PointA, true);
            (float floorZ_b, float _) = MathFormulas.CalculateZAtPoint(neighborSector, portalWall.Wall.PointB, true);


            int neighborFloor = (int)MathF.Max(floorZ_a, floorZ_b);

            Debug.Assert(wall.Neighbor.HasValue);
            RenderableSector sector = wall.Sector;
            int lowerSectorHeight = neighborFloor - sector.Floor;
            int sectorHeight = sector.Ceil - sector.Floor;
            int wallFromXOffset = portalWall.Offset;
            int wallFromX = portalWall.XLeft;
            int wallToX = portalWall.XRight;

            (int textureHeight, float scaledTextureHeight) = CalculateScale();
            int textureHeightShifted = textureHeight << 16;

            float scale = lowerSectorHeight / (float)sectorHeight;
            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(wall, wallFromXOffset, scale);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            CalculateLowerTextureYIncrementCore(
                portalTo, portalToClamped, ceil, startingYTexturePosition, textureYIncrement,
                wallFromX, wallToX, textureStart, textureHeightShifted, scaledTextureHeight,
                wallStartY, ceilDistIncr, wallEndY, floorDistIncr);

            return;

            (int Height, float ScaledTextureHeight) CalculateScale()
            {
                int textureHeight = textureInfo.Height;

                float scaledTextureHeight;

                if (textureInfo.YScale is { } yScale)
                {
                    yScale = lowerSectorHeight * yScale;
                    scaledTextureHeight = (textureHeight << 16) * yScale;
                }
                else
                {
                    scaledTextureHeight =lowerSectorHeight << 16;
                }

                return (textureHeight, scaledTextureHeight);
            }
        }

        private static void CalculateLowerTextureYIncrementCore(
            int* portalTo,
            int* portalToClamped,
            int* ceil,
            int* startingYTexturePosition,
            int* textureYIncrement,
            int wallFromX,
            int wallToX,
            int textureStart,
            int textureHeightShifted,
            float scaledTextureHeight,
            float wallStartY,
            float ceilDistIncr,
            float wallEndY,
            float floorDistIncr)
        {
            int length = wallToX - wallFromX;

            if (Vector.IsHardwareAccelerated && length > Vector<float>.Count)
            {
                int vCount = Vector<float>.Count;
                int rem = length & (vCount - 1);
                wallToX -= rem;

                Vector<float> scaledTextureHeightV = Vector.Create(scaledTextureHeight);
                Vector<float> textureStartV = Vector.Create((float)textureStart);

                Vector<float> ceilDistIncrV = Vector.Create(ceilDistIncr * Vector<float>.Count);
                Vector<float> floorDistIncrV = Vector.Create(floorDistIncr * Vector<float>.Count);
                Vector<float> wallStartYv = Vector.CreateSequence(wallStartY, ceilDistIncr);
                Vector<float> wallEndYv = Vector.CreateSequence(wallEndY, floorDistIncr);

                for (int x = wallFromX; x < wallToX; x += vCount)
                {
                    Vector<int> portalToYv = Vector.Load(portalTo + x);
                    Vector<int> portalToSlopedYv = Vector.Load(portalToClamped + x);
                    Vector<int> ceilYv = Vector.Load(ceil + x);

                    Vector<int> ceilDiffV = ceilYv - portalToSlopedYv;
                    Vector<float> topOffsetV = Vector.Max(Vector.ConvertToSingle(ceilDiffV), Vector<float>.Zero);

                    topOffsetV += Vector.ConvertToSingle(portalToSlopedYv - portalToYv);

                    Vector<float> textureYIncrV = scaledTextureHeightV / (wallEndYv - wallStartYv);
                    topOffsetV *= textureYIncrV;

                    Vector<float> textureYPosYv = textureStartV + topOffsetV;
                    Vector<int> textureYPosYIntV = Vector.ConvertToInt32Native(textureYPosYv);

                    for (int i = 0; i < vCount; i++)
                    {
                        startingYTexturePosition[x + i] = SharedHelpers.EnsureOffsetIsPositive(textureHeightShifted, textureYPosYIntV[i]);
                    }

                    Vector.Store(Vector.ConvertToInt32Native(textureYIncrV), textureYIncrement + x);

                    wallStartYv += ceilDistIncrV;
                    wallEndYv += floorDistIncrV;
                }

                wallStartY = wallStartYv[0];
                wallEndY = wallEndYv[0];

                wallFromX = wallToX;
                wallToX += rem;
            }

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
                textureYPosY = SharedHelpers.EnsureOffsetIsPositive(textureHeightShifted, textureYPosY);

                Debug.Assert(textureYPosY < textureHeightShifted);

                startingYTexturePosition[x] = textureYPosY;
                textureYIncrement[x] = float.ConvertToIntegerNative<int>(textureYIncr);

                wallStartY += ceilDistIncr;
                wallEndY += floorDistIncr;
            }
        }

        private void CalculateDistance(RenderablePortalWall renderableWall)
        {
            float* distance = memoryPool.GetBucketPtr<float>(MemoryPoolBucket.Distance);

            RenderableWall wall = renderableWall.Wall;

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, PixelWidth, wallFromX);

            int length = wallToX - wallFromX;

            if (Vector.IsHardwareAccelerated && length > Vector<float>.Count)
            {
                float* cameraRaySpan = stackalloc float[Vector<float>.Count];

                int vCount = Vector<float>.Count;
                int rem = length & (vCount - 1);
                wallToX -= rem;

                Vector<float> t1V = Vector.Create(t1);
                Vector<float> d2yV = Vector.Create(d2y);
                Vector<float> negD2xV = Vector.Create(-d2x);

                for (int x = wallFromX; x < wallToX; x += vCount)
                {
                    // precision seems critical here
                    // so we fall back to scalar math here
                    // Vector.CreateSequence and cameraRayV + strideV produce
                    // a slightly different result
                    for (int i = 0; i < Vector<float>.Count; i++)
                    {
                        cameraRaySpan[i] = cameraRay;
                        cameraRay += cameraWidthIncr;
                    }

                    Vector<float> cameraRayV = Vector.Load(cameraRaySpan);
                    Vector<float> denominatorV = Vector.FusedMultiplyAdd(cameraRayV, d2yV, negD2xV);
                    Vector<float> resultV = t1V / denominatorV;
                    Vector.Store(resultV, distance + x);
                }

                wallFromX = wallToX;
                wallToX += rem;
            }

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
            {
                distance[x] = MathFormulas.CalculateDistance2(cameraRay, t1, d2y, d2x);
            }
        }

        private void CalculateTextureDistanceAndXPosition(RenderablePortalWall renderableWall, GameTextureInfo textureInfo)
        {
            float* cameraRaySpan = stackalloc float[Vector<float>.Count];
            int* xLocation = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.TextureXLocation);
            float* distance = memoryPool.GetBucketPtr<float>(MemoryPoolBucket.Distance);

            int width = PixelWidth;

            RenderableWall wall = renderableWall.Wall;

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;


            int textureHeight = textureInfo.Height;
            int textureWidth = textureInfo.Width;
            float xOffset = SharedHelpers.EnsureOffsetIsPositive(textureInfo.Width, textureInfo.XOffset);

            if (textureInfo.XScale is { } xScale)
            {
                float wallLength = wall.Length;
                xScale = xScale / wallLength * textureWidth;
            }
            else
            {
                xScale = 1f;
            }

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);

            bool flipX = wall.Flipped;
            
            if (textureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.MirrorX))
            {
                flipX = !flipX;
            }

            float rX = flipX ? wall.R2.X : wall.R1.X;
            float rY = flipX ? wall.R2.Y : wall.R1.Y;

            if (SharedHelpers.IsPowerOfTwo(textureWidth))
            {
                PowerOfTwo();
            }
            else
            {
                OddTextureWidth();
            }

            return;

            void PowerOfTwo()
            {
                int textureWidthMask = textureWidth - 1;
                int length = wallToX - wallFromX;

                if (Vector.IsHardwareAccelerated && length > Vector<float>.Count)
                {
                    int vCount = Vector<float>.Count;
                    int rem = length & (vCount - 1);
                    wallToX -= rem;

                    Vector<float> t1V = Vector.Create(t1);
                    Vector<float> d2yV = Vector.Create(d2y);
                    Vector<float> d2xV = Vector.Create(d2x);
                    Vector<float> rXV = Vector.Create(rX);
                    Vector<float> rYV = Vector.Create(rY);
                    Vector<float> xScaleV = Vector.Create(xScale);
                    Vector<float> xOffsetV = Vector.Create(xOffset);
                    Vector<int> textureWidthMaskV = Vector.Create(textureWidthMask);
                    Vector<int> textureHeightV = Vector.Create(textureHeight);

                    for (int x = wallFromX; x < wallToX; x += vCount)
                    {
                        // precision seems critical here
                        // so we fall back to scalar math here
                        // Vector.CreateSequence and cameraRayV + strideV produce
                        // a slightly different result
                        for (int i = 0; i < Vector<float>.Count; i++)
                        {
                            cameraRaySpan[i] = cameraRay;
                            cameraRay += cameraWidthIncr;
                        }
                        Vector<float> cameraRayV = Vector.Load(cameraRaySpan);

                        (Vector<float> fromToXdistV, Vector<float> fromToYdistV) = MathFormulas.CalculateRayIntersection(cameraRayV, t1V, d2yV, d2xV);

                        Vector<float> distXV = rXV - fromToXdistV;
                        Vector<float> distYV = rYV - fromToYdistV;

                        Vector<float> textureDistV = Vector.SquareRoot(distXV * distXV + distYV * distYV);
                        Vector<int> textureDistIntV = Vector.ConvertToInt32Native(Vector.FusedMultiplyAdd(textureDistV, xScaleV, xOffsetV));
                        Vector<int> xLocationV = (textureDistIntV & textureWidthMaskV) * textureHeightV;

                        Vector.Store(xLocationV, xLocation + x);
                        Vector.Store(fromToYdistV, distance + x);
                    }

                    wallFromX = wallToX;
                    wallToX += rem;
                }

                for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
                {
                    (float fromToXdist, float fromToYdist) = MathFormulas.CalculateRayIntersection(cameraRay, t1, d2y, d2x);

                    float distX = rX - fromToXdist;
                    float distY = rY - fromToYdist;

                    float textureDist = MathF.Sqrt(distX * distX + distY * distY);
                    int textureDistInt = float.ConvertToIntegerNative<int>(MathF.FusedMultiplyAdd(textureDist, xScale, xOffset));
                    xLocation[x] = (textureDistInt & textureWidthMask) * textureHeight;

                    Debug.Assert(xLocation[x] >= 0);

                    distance[x] = fromToYdist;
                }
            }

            void OddTextureWidth()
            {
                int length = wallToX - wallFromX;

                if (Vector.IsHardwareAccelerated && length > Vector<float>.Count)
                {
                    int vCount = Vector<float>.Count;
                    int rem = length & (vCount - 1);
                    wallToX -= rem;

                    Vector<float> t1V = Vector.Create(t1);
                    Vector<float> d2yV = Vector.Create(d2y);
                    Vector<float> d2xV = Vector.Create(d2x);
                    Vector<float> rXV = Vector.Create(rX);
                    Vector<float> rYV = Vector.Create(rY);
                    Vector<float> xScaleV = Vector.Create(xScale);
                    Vector<float> xOffsetV = Vector.Create(xOffset);

                    for (int x = wallFromX; x < wallToX; x += vCount)
                    {
                        // precision seems critical here
                        // so we fall back to scalar math here
                        // Vector.CreateSequence and cameraRayV + strideV produce
                        // a slightly different result
                        for (int i = 0; i < Vector<float>.Count; i++)
                        {
                            cameraRaySpan[i] = cameraRay;
                            cameraRay += cameraWidthIncr;
                        }
                        Vector<float> cameraRayV = Vector.Load(cameraRaySpan);

                        (Vector<float> fromToXdistV, Vector<float> fromToYdistV) = MathFormulas.CalculateRayIntersection(cameraRayV, t1V, d2yV, d2xV);

                        Vector<float> distXV = rXV - fromToXdistV;
                        Vector<float> distYV = rYV - fromToYdistV;

                        Vector<float> textureDistV = Vector.SquareRoot(distXV * distXV + distYV * distYV);
                        Vector<int> textureDistIntV = Vector.ConvertToInt32Native(Vector.FusedMultiplyAdd(textureDistV, xScaleV, xOffsetV));

                        for (int i = 0; i < Vector<float>.Count; i++)
                        {
                            xLocation[x + i] = (textureDistIntV[i] % textureWidth) * textureHeight;
                        }

                        Vector.Store(fromToYdistV, distance + x);
                    }

                    wallFromX = wallToX;
                    wallToX += rem;
                }

                for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr)
                {
                    (float fromToXdist, float fromToYdist) = MathFormulas.CalculateRayIntersection(cameraRay, t1, d2y, d2x);

                    float distX = rX - fromToXdist;
                    float distY = rY - fromToYdist;

                    float textureDist = MathF.Sqrt(distX * distX + distY * distY);
                    int textureDistInt = float.ConvertToIntegerNative<int>(MathF.FusedMultiplyAdd(textureDist, xScale, xOffset));
                    xLocation[x] = (textureDistInt % textureWidth) * textureHeight;

                    Debug.Assert(xLocation[x] >= 0);

                    distance[x] = fromToYdist;
                }
            }
        }

        private void CalculatePortalClamp(RenderablePortalWall renderableWall)
        {
            RenderColumnStatus* status = memoryPool.GetBucketPtr<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);

            int* wallStartClamped = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.WallStartClamped);
            int* wallEndClamped = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.WallEndClamped);

            int* portalFromClamped = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalFromClamped);
            int* portalToClamped = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalToClamped);

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int length = wallToX - wallFromX;

            if (Vector.IsHardwareAccelerated && length > Vector<int>.Count)
            {
                int rem = length & (Vector<int>.Count - 1);
                wallToX -= rem;

                Vector<int> finishedFlag = Vector.Create((int)RenderColumnStatus.FinishedRendering);

                for (int x = wallFromX; x < wallToX; x += Vector<int>.Count)
                {
                    Vector<int> statusV = Vector.Load((int*)(status + x));
                    Vector<int> wallStartV = Vector.Load(wallStartClamped + x);
                    Vector<int> wallEndV = Vector.Load(wallEndClamped + x);
                    Vector<int> portalFromV = Vector.Load(portalFromClamped + x);
                    Vector<int> portalToV = Vector.Load(portalToClamped + x);

                    Vector<int> clampedFrom = Vector.ClampNative(portalFromV, wallStartV, wallEndV);
                    Vector<int> clampedTo = Vector.ClampNative(portalToV, wallStartV, wallEndV);

                    // keep original values for finished columns, use clamped elsewhere
                    Vector<int> isFinished = Vector.Equals(statusV & finishedFlag, finishedFlag);

                    Vector.Store(Vector.ConditionalSelect(isFinished, portalFromV, clampedFrom), portalFromClamped + x);
                    Vector.Store(Vector.ConditionalSelect(isFinished, portalToV, clampedTo), portalToClamped + x);
                }

                wallFromX = wallToX;
                wallToX += rem;
            }

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
            RenderColumnStatus* statusPtr = memoryPool.GetBucketPtr<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);

            int* wallStartClampedPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.WallStartClamped);
            int* wallEndClampedPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.WallEndClamped);
            int* ceilingStartPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.CeilingStart);
            int* floorEndPtr = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.FloorEnd);

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            int length = wallToX - wallFromX;

            if (Vector.IsHardwareAccelerated && length > Vector<int>.Count)
            {
                int rem = length & (Vector<int>.Count - 1);
                wallToX -= rem;

                Vector<int> wallMask = Vector.Create((int)(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderWall));

                for (int x = wallFromX; x < wallToX; x += Vector<int>.Count)
                {
                    Vector<int> statusV = Vector.Load((int*)(statusPtr + x));

                    Vector<int> ceilingStartY = Vector.Load(ceilingStartPtr + x);
                    Vector<int> floorEndY = Vector.Load(floorEndPtr + x);

                    Vector<int> wallStartY = Vector.Load(wallStartClampedPtr + x);
                    Vector<int> wallEndY = Vector.Load(wallEndClampedPtr + x);

                    Vector<int> clamptedFromY = Vector.ClampNative(wallStartY, ceilingStartY, floorEndY);
                    Vector<int> clamptedToY = Vector.ClampNative(wallEndY, ceilingStartY, floorEndY);

                    Vector.Store(clamptedFromY, wallStartClampedPtr + x);
                    Vector.Store(clamptedToY, wallEndClampedPtr + x);

                    // no need for conditional select to preserve already-finished lanes:
                    // WallStartClamped/WallEndClamped are only ever read for columns that are
                    // still Calculated (FinishedRendering never coexists with Calculated), so
                    // overwriting them here for already-finished lanes is harmless
                    statusV &= wallMask;
                    Vector<int> notRenderable = ~Vector.Equals(statusV, wallMask);
                    statusV = notRenderable | Vector.GreaterThanOrEqual(clamptedFromY, clamptedToY);

                    for (int i = 0; i < Vector<int>.Count; i++)
                    {
                        if (statusV[i] != 0)
                        {
                            statusPtr[x + i] = RenderColumnStatus.FinishedRendering;
                        }
                    }
                }

                wallFromX = wallToX;
                wallToX += rem;
            }


            for (int x = wallFromX; x <= wallToX; x++)
            {
                if (statusPtr[x].IsFinished)
                {
                    continue;
                }

                int ceilingStartY = ceilingStartPtr[x];
                int floorEndY = floorEndPtr[x];

                int wallStartY = wallStartClampedPtr[x];
                int wallEndY = wallEndClampedPtr[x];

                int clamptedFromY = Math.Clamp(wallStartY, ceilingStartY, floorEndY);
                int clamptedToY = Math.Clamp(wallEndY, ceilingStartY, floorEndY);

                Debug.Assert(ceilingStartY >= 0);
                Debug.Assert(floorEndY >= 0);
                wallStartClampedPtr[x] = clamptedFromY;
                wallEndClampedPtr[x] = clamptedToY;

                if (!statusPtr[x].WallRenderable || clamptedFromY >= clamptedToY)
                {
                    statusPtr[x] = RenderColumnStatus.FinishedRendering;
                }
            }
        }

        private void CalculateNewFloorCeiling(int from, int to)
        {
            int* portalFromClamped = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalFromClamped);
            int* portalToClamped = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalToClamped);

            int* ceilingStart = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.CeilingStart);
            int* floorEnd = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.FloorEnd);

            RenderColumnStatus* status = memoryPool.GetBucketPtr<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);

            int length = to - from;

            if (Vector.IsHardwareAccelerated && length > Vector<int>.Count)
            {
                int rem = length & (Vector<int>.Count - 1);
                to -= rem;

                Vector<int> wallRenderableMask = Vector.Create((int)(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderWall));
                Vector<int> canRenderWallV = Vector.Create((int)RenderColumnStatus.CanRenderWall);
                Vector<int> finishedFlagV = Vector.Create((int)RenderColumnStatus.FinishedRendering);

                for (int x = from; x < to; x += Vector<int>.Count)
                {
                    Vector<int> statusV = Vector.Load((int*)(status + x));
                    Vector<int> portalFromV = Vector.Load(portalFromClamped + x);
                    Vector<int> portalToV = Vector.Load(portalToClamped + x);

                    Vector<int> isRenderable = Vector.Equals(Vector.BitwiseAnd(statusV, wallRenderableMask), wallRenderableMask);
                    Vector<int> isNotRenderable = ~isRenderable;

                    // no need for conditional select to match scalar option,
                    Vector.Store(portalFromV, ceilingStart + x);
                    Vector.Store(portalToV, floorEnd + x);

                    Vector<int> newStatusV = Vector.ConditionalSelect(isNotRenderable, finishedFlagV, statusV ^ canRenderWallV);
                    Vector.Store(newStatusV, (int*)(status + x));
                }

                from = to;
                to += rem;
            }

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

            Debug.Assert(!canRenderPortal);

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

        private Span<ushort> DetermineMaxHorizontalRenderingDistance(RenderablePortalWall renderableWall, uint* wallStartPtr, uint* wallEndPtr, bool usePrimaryTempBuckets)
        {
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            Span<ushort> repeatedCount = memoryPool.GetBucket<ushort>(usePrimaryTempBuckets ? MemoryPoolBucket.Temp : MemoryPoolBucket.Temp3)[wallFromX..(wallToX + 1)];
            repeatedCount.Fill((ushort)repeatedCount.Length);

            // set repeat count to 0 where there is nothing to draw
            RenderColumnStatus* statusPtr = memoryPool.GetBucketPtr<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            statusPtr += wallFromX;

            for (int x = wallFromX; x <= wallToX; x++)
            {
                uint wallStart = wallStartPtr[x];
                uint wallEnd = wallEndPtr[x];

                if (!(*statusPtr).WallRenderable || wallStart >= wallEnd)
                {
                    repeatedCount[x - wallFromX] = 0;
                }

                statusPtr++;
            }

            return repeatedCount;
        }

        protected abstract ushort* CalculateTransparentWall(
            ReadOnlySpan<RenderableSector> sectors,
            RenderWindowWallSnapshot renderableWall);

        #region Pre Calculate

        private void PrecalculateBasicWallDistance(RenderablePortalWall renderableWall)
        {
            Debug.Assert(renderableWall.Wall.MiddleTexture != null);

            CalculateUpperTextureYIncrement(renderableWall, renderableWall.Wall.MiddleTexture);
            CalculateWallClamp(renderableWall);
            CalculateTextureDistanceAndXPosition(renderableWall, renderableWall.Wall.MiddleTexture!);
        }

        private static (int Height, float ScaledTextureHeight) CalculateScale(
            RenderableSector sector,
            GameTextureInfo wallTexture)
        {
            int textureHeight = wallTexture.Height;

            float scaledTextureHeight;

            if (wallTexture.YScale is { } yScale)
            {
                yScale = (sector.Ceil - sector.Floor) * yScale;
                scaledTextureHeight = (textureHeight << 16) * yScale;
            }
            else
            {
                scaledTextureHeight = (sector.Ceil - sector.Floor) << 16;
            }

            return (textureHeight, scaledTextureHeight);
        }

        #endregion

        #region Calculation Helpers

        private static (bool RenderLower, bool RenderUpper, bool IsBasicWall) CalculateCanRenderPortalWall(ReadOnlySpan<RenderableSector> sectors, RenderableWall wall)
        {
            Debug.Assert(wall.Neighbor != null);

            RenderableSector sector = wall.Sector;
            RenderableSector neighborSector = sectors[wall.Neighbor.Value];
            bool wallSloped = sector.Settings.Sloped || neighborSector.Settings.Sloped;

            bool renderLower, renderUpper, basicWall;
            float sectorHeight, ceilOffset, floorOffset;

            if (wallSloped)
            {
                (float floorZ_a, float ceilingZ_a) = CalculateZAtPoint(sector, wall.R1);
                (float floorZ_b, float ceilingZ_b) = CalculateZAtPoint(sector, wall.R2);

                (float p_floorZ_a, float p_ceilingZ_a) = CalculateZAtPoint(neighborSector, wall.R1);
                (float p_floorZ_b, float p_ceilingZ_b) = CalculateZAtPoint(neighborSector, wall.R2);

                (sectorHeight, ceilOffset, floorOffset) = CalculatePortalOffsets(floorZ_a, ceilingZ_a, p_floorZ_a, p_ceilingZ_a);

                renderLower = floorOffset != 0;
                renderUpper = ceilOffset != 0;
                basicWall = !(floorOffset == sectorHeight || sectorHeight == -ceilOffset);

                (sectorHeight, ceilOffset, floorOffset) = CalculatePortalOffsets(floorZ_b, ceilingZ_b, p_floorZ_b, p_ceilingZ_b);

                renderLower |= floorOffset != 0;
                renderUpper |= ceilOffset != 0;
                basicWall |= (floorOffset == sectorHeight || sectorHeight == -ceilOffset);

                return (renderLower, renderUpper, basicWall);
            }
            else
            {
                (sectorHeight, ceilOffset, floorOffset) = CalculatePortalOffsets(sectors, wall);

                renderLower = floorOffset != 0;
                renderUpper = ceilOffset != 0;
                basicWall = !(floorOffset == sectorHeight || sectorHeight == -ceilOffset);

                return (renderLower, renderUpper, basicWall);
            }
        }

        protected static (float SectorHeight, float CeilingOffset, float FloorOffset) CalculatePortalOffsets(ReadOnlySpan<RenderableSector> sectors, RenderableWall wall)
        {
            Debug.Assert(wall.Neighbor != null);

            RenderableSector sector = wall.Sector;
            RenderableSector neighborSector = sectors[wall.Neighbor.Value];

            return CalculatePortalOffsets(sector.Floor, sector.Ceil, neighborSector.Floor, neighborSector.Ceil);
        }

        private static (float SectorHeight, float CeilingOffset, float FloorOffset) CalculatePortalOffsets(float floorA, float ceilA, float floorB, float ceilB)
        {
            float sectorHeight = ceilA - floorA;
            float floorOffset = floorB - floorA;
            float ceilOffset = ceilB - ceilA;

            if (floorOffset < 0f)
            {
                floorOffset = 0f;
            }

            if (ceilOffset > 0f)
            {
                ceilOffset = 0f;
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

        #endregion

    }
}
