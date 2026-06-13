using RenderingEngine.Tooling;
using SoftwareRendererModels;
using System.Numerics;
namespace RenderingEngine.Engine
{
    internal sealed unsafe partial class PortalRenderer
    {
        private void CalculateUpperTextureYIncrement(RenderablePortalWall portalWall, GameTextureInfo textureInfo)
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

        private void CalculateLowerTextureYIncrement(RenderablePortalWall portalWall, GameTextureInfo textureInfo)
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
            float* distance = memoryPool.GetBucketPtr<float>(MemoryPoolBucket.Distance);

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

        private void CalculateTextureDistanceAndXPosition(RenderablePortalWall renderableWall, GameTextureInfo textureInfo)
        {
            Span<int> xLocation = memoryPool.GetBucket<int>(MemoryPoolBucket.TextureXLocation);

            int width = PixelWidth;

            Span<float> distance = memoryPool.GetBucket<float>(MemoryPoolBucket.Distance);

            RenderableWall wall = renderableWall.Wall;

            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;


            int textureHeight = textureInfo.Height;
            int textureWidth = textureInfo.Width;
            float xOffset = SharedHelpers.EnsureOffsetIsPositive(textureInfo.Width, textureInfo.XOffset);

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

            bool flipX = wall.Flipped;
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

                Debug.Assert(xLocation[x] >= 0);

                distance[x] = fromToYdist;
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

                    // limit to Calculated CanRenderWall Mask
                    statusV &= Vector.Create((int)(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderWall));
                    // check where can render wall is false
                    statusV = Vector.LessThan(statusV, Vector<int>.Zero);
                    // OR where fromY >= toY
                    statusV |= Vector.GreaterThanOrEqual(clamptedFromY, clamptedToY);

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

        private Span<ushort> DetermineMaxHorizontalRenderingDistance(RenderablePortalWall renderableWall, uint* wallStartPtr, uint* wallEndPtr)
        {
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;
            ushort length = (ushort)(wallToX - wallFromX + 1);

            Span<ushort> repeatedCount = memoryPool.GetBucket<ushort>(MemoryPoolBucket.Temp)[..length];
            repeatedCount.Fill(length);

            // set repeat count to 0 where there is nothing to draw
            ref RenderColumnStatus statusRef = ref memoryPool.GetBucketRef<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            statusRef = ref Unsafe.Add(ref statusRef, wallFromX);

            for (int x = wallFromX; x <= wallToX; x++)
            {
                uint wallStart = wallStartPtr[x];
                uint wallEnd = wallEndPtr[x];

                if (!statusRef.WallRenderable || wallStart >= wallEnd)
                {
                    repeatedCount[x - wallFromX] = 0;
                }

                statusRef = ref Unsafe.Add(ref statusRef, 1);
            }

            return repeatedCount;
        }

        private ushort* CalculateTransparentWallDoom(
            ReadOnlySpan<RenderableSector> sectors,
            RenderWindowWallSnapshot renderableWall)
        {
            int width = PixelWidth;

            int bufferOffset = PixelWidth * (renderableWall.Depth + 1);

            Span<float> distanceSpan = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance)[bufferOffset..];
            Span<RenderColumnStatus> columnStatus = spriteCacheMemoryPool.GetBucket<RenderColumnStatus>(SpriteCachePoolBucket.RenderStatus)[bufferOffset..];

            Span<int> wallStart = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[bufferOffset..];
            Span<int> wallEnd = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[bufferOffset..];

            // RenderOutline(wallStart, wallEnd, BGRA.Red, BGRA.Green);//, renderableWall.XLeft, renderableWall.XRight);

            RenderableWall wall = renderableWall.Wall;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);


            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            (float sectorHeight, float ceilOffset, float floorOffset) = CalculatePortalOffsets(sectors, renderableWall.Wall);
            float oneOverSectorHeight = 1f / sectorHeight;

            // Texture Calculations
            GameTextureInfo? textureInfo = wall.MiddleTexture;
            Debug.Assert(textureInfo != null);
            GameTexture texture = TextureCache.GetTexture(textureInfo);
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;
            bool texHeightDivisible2 = SharedHelpers.IsPowerOfTwo(textureHeight);
            if (texHeightDivisible2)
            {
                textureHeight--;
            }

            int xOffset = SharedHelpers.EnsureOffsetIsPositive(textureInfo.Width, textureInfo.XOffset);
            int yOffset = textureInfo.YOffset > sectorHeight ? textureInfo.YOffset - 65536 : textureInfo.YOffset;

            int* textureXLocationPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.TextureXLocation);
            uint* textureYLocationPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.StartingYTexturePosition);
            uint* textureYIncrementPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureYIncrement);

            int* portalFromClampedPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalFromClamped);
            int* portalToClampedPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalToClamped);
            ushort* repeatedCountPtr = this.memoryPool.GetBucketPtr<ushort>(MemoryPoolBucket.Temp);

            int length = wallToX - wallFromX;

            bool renderFromTop = textureInfo.RenderingOptions.HasFlag(TextureRenderingOptions.FromTop);


            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr, wallStartY += ceilDistIncr, wallEndY += floorDistIncr)
            {
                RenderColumnStatus status = columnStatus[x];

                if (status.PortalRenderable)
                {
                    repeatedCountPtr[x - wallFromX] = 0;
                    continue;
                }

                float dist = distanceSpan[x];
                int floorEnd = wallEnd[x];
                int ceilingStart = wallStart[x];

                (float distance, float fromToYdist) = MathFormulas.CalculateDistance(wall, cameraRay, t1, d2y, d2x);

                if (fromToYdist > dist)
                {
                    repeatedCountPtr[x - wallFromX] = 0;
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                float pixelsPerUnit = (wallEndY - wallStartY) * oneOverSectorHeight;

                // Portal Calculation
                float floorPixelOffset = pixelsPerUnit * floorOffset;
                float ceilPixelOffset = pixelsPerUnit * ceilOffset;
                float portalFromY = wallStartY - ceilPixelOffset;
                float portalToY = wallEndY - floorPixelOffset;

                float textureStartY = renderFromTop ? portalFromY : (portalToY - texture.Height * pixelsPerUnit);
                float textureEndY = renderFromTop ? (portalFromY + texture.Height * pixelsPerUnit) : portalToY;

                if (yOffset != 0)
                {
                    float yOffsetF = yOffset * pixelsPerUnit;

                    if (yOffset > 0)
                    {
                        textureStartY = renderFromTop ? textureStartY + yOffsetF : textureStartY - yOffsetF;
                        textureEndY = renderFromTop ? textureEndY + yOffsetF : textureEndY - yOffsetF;
                    }
                    else
                    {
                        // TODO: same expression in true and false branch
                        textureStartY = renderFromTop ? textureStartY - yOffsetF : textureStartY - yOffsetF;
                        textureEndY = renderFromTop ? textureEndY - yOffsetF : textureEndY - yOffsetF;
                    }
                }

                int textureStartYClamped = Math.Clamp(float.ConvertToIntegerNative<int>(textureStartY), ceilingStart, floorEnd);
                int textureEndYClamped = Math.Clamp(float.ConvertToIntegerNative<int>(textureEndY), ceilingStart, floorEnd);

                if (textureStartYClamped >= textureEndYClamped)
                {
                    repeatedCountPtr[x - wallFromX] = 0;
                    continue;
                }

                float offset = textureStartYClamped - textureStartY;

                // Calculate Middle Texture Position
                float textureYIncr = sectorHeight / (wallEndY - wallStartY);
                int textureXPos = ((float.ConvertToIntegerNative<int>(distance) + xOffset) % textureHeight) * textureWidth;
                float textureYPos = MathF.FusedMultiplyAdd(textureYIncr, offset, textureWidth);


                textureYIncr *= (1 << 16);
                textureYPos *= (1 << 16);

                portalFromClampedPtr[x] = textureStartYClamped;
                portalToClampedPtr[x] = textureEndYClamped;

                textureXLocationPtr[x] = textureXPos;
                textureYLocationPtr[x] = float.ConvertToIntegerNative<uint>(textureYPos);
                textureYIncrementPtr[x] = float.ConvertToIntegerNative<uint>(textureYIncr);
                repeatedCountPtr[x - wallFromX] = (ushort)length;
            }

            return repeatedCountPtr;
        }

        private unsafe ushort* CalculateTransparentWallBuild(
            ReadOnlySpan<RenderableSector> sectors,
            RenderWindowWallSnapshot renderableWall)
        {
            int width = PixelWidth;

            int offset = PixelWidth * renderableWall.Depth;

            Span<float> spriteDistance = spriteCacheMemoryPool.GetBucket<float>(SpriteCachePoolBucket.Distance)[offset..];
            Span<RenderColumnStatus> columnStatus = spriteCacheMemoryPool.GetBucket<RenderColumnStatus>(SpriteCachePoolBucket.RenderStatus)[offset..];

            if (renderableWall.Depth > 1)
            {
                offset = PixelWidth * (renderableWall.Depth - 1);
            }

            Span<int> wallStart = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallStart)[offset..];
            Span<int> wallEnd = spriteCacheMemoryPool.GetBucket<int>(SpriteCachePoolBucket.WallEnd)[offset..];

            RenderableWall wall = renderableWall.Wall;
            int wallFromXOffset = renderableWall.Offset;
            int wallFromX = renderableWall.XLeft;
            int wallToX = renderableWall.XRight;

            (float cameraRay, float cameraWidthIncr, float t1, float d2y, float d2x) = MathFormulas.CalculateCameraRay(wall, width, wallFromX);


            RenderablePlaneInfo yPlaneInfo = MathFormulas.CalculateLeftWallYPlaneInfo(wall, wallFromXOffset);
            float wallStartY = yPlaneInfo.WallStartY;
            float ceilDistIncr = yPlaneInfo.CeilDistIncr;
            float wallEndY = yPlaneInfo.WallEndY;
            float floorDistIncr = yPlaneInfo.FloorDistIncr;

            (float sectorHeight, float ceilOffset, float floorOffset) = CalculatePortalOffsets(sectors, renderableWall.Wall);
            float oneOverSectorHeight = 1f / sectorHeight;

            // Texture Calculations
            GameTextureInfo? textureInfo = wall.MiddleTexture;
            Debug.Assert(textureInfo != null);
            GameTexture texture = TextureCache.GetTexture(textureInfo);
            int textureWidth = texture.Height;
            int textureHeight = texture.Width;
            bool texHeightDivisible2 = SharedHelpers.IsPowerOfTwo(textureHeight);
            if (texHeightDivisible2)
            {
                textureHeight--;
            }
            int xOffset = textureInfo.XOffset;
            int yOffset = textureInfo.YOffset;
            RenderableSector sector = wall.Sector;
            (float xScale, float yScale) = (textureInfo.XScale!.Value, textureInfo.YScale!.Value);
            yScale = (sector.Ceil - sector.Floor) * yScale;
            xScale = xScale / wall.Length * texture.Width;

            int* textureXLocationPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.TextureXLocation);
            uint* textureYLocationPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.StartingYTexturePosition);
            uint* textureYIncrementPtr = this.memoryPool.GetBucketPtr<uint>(MemoryPoolBucket.TextureYIncrement);

            int* portalFromClampedPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalFromClamped);
            int* portalToClampedPtr = this.memoryPool.GetBucketPtr<int>(MemoryPoolBucket.PortalToClamped);
            ushort* repeatedCountPtr = this.memoryPool.GetBucketPtr<ushort>(MemoryPoolBucket.Temp);

            int length = wallToX - wallFromX;

            for (int x = wallFromX; x <= wallToX; x++, cameraRay += cameraWidthIncr, wallStartY += ceilDistIncr, wallEndY += floorDistIncr)
            {
                RenderColumnStatus columnStatusY = columnStatus[x];

                if (columnStatusY.PortalRenderable)
                {
                    repeatedCountPtr[x - wallFromX] = 0;
                    continue;
                }

                float distance = spriteDistance[x];
                int floorEndY = wallEnd[x];
                int ceilingStartY = wallStart[x];

                (float distanceY, float fromToYdist) = MathFormulas.CalculateDistance(wall, cameraRay, t1, d2y, d2x);

                if (fromToYdist > distance)
                {
                    repeatedCountPtr[x - wallFromX] = 0;
                    wallStartY += ceilDistIncr;
                    wallEndY += floorDistIncr;
                    continue;
                }

                float pixelsPerUnit = (wallEndY - wallStartY) * oneOverSectorHeight;

                // Portal Calculation
                float floorPixelOffset = pixelsPerUnit * floorOffset;
                float ceilPixelOffset = pixelsPerUnit * ceilOffset;
                float textureFromY = wallStartY - ceilPixelOffset;
                float textureToY = wallEndY - floorPixelOffset;

                // Clamp to View Window
                int clampedFromY = Math.Clamp(float.ConvertToIntegerNative<int>(textureFromY), ceilingStartY, floorEndY);
                int clampedToY = Math.Clamp(float.ConvertToIntegerNative<int>(textureToY), ceilingStartY, floorEndY);

                if (clampedFromY >= clampedToY)
                {
                    repeatedCountPtr[x - wallFromX] = 0;
                    continue;
                }

                // Calculate Middle Texture Position
                int textureXPos = float.ConvertToIntegerNative<int>(distanceY * xScale);
                textureXPos += xOffset;
                textureXPos = texHeightDivisible2 ? (textureXPos & textureHeight) : (textureXPos % textureHeight);
                textureXPos *= textureWidth;

                float textureYIncr = (textureWidth * yScale) / (wallEndY - wallStartY);
                float textureYPos = yOffset - textureYIncr * (wallStartY - clampedFromY);

                textureYIncr *= (1 << 16);
                textureYPos *= (1 << 16);

                portalFromClampedPtr[x] = clampedFromY;
                portalToClampedPtr[x] = clampedToY;

                textureXLocationPtr[x] = textureXPos;
                textureYLocationPtr[x] = float.ConvertToIntegerNative<uint>(textureYPos);
                textureYIncrementPtr[x] = float.ConvertToIntegerNative<uint>(textureYIncr);
                repeatedCountPtr[x - wallFromX] = (ushort)length;
            }

            return repeatedCountPtr;
        }



        #region Pre Calculate

        private void PrecalculateBasicWallDistance(RenderablePortalWall renderableWall)
        {
            Debug.Assert(renderableWall.Wall.MiddleTexture != null);

            CalculateUpperTextureYIncrement(renderableWall, renderableWall.Wall.MiddleTexture);
            CalculateWallClamp(renderableWall);
            CalculateTextureDistanceAndXPosition(renderableWall, renderableWall.Wall.MiddleTexture!);
        }

        public static bool TextureIsUntiledY(RenderableSector sector,
            GameTextureInfo wallTexture)
        {
            int textureHeight = wallTexture.Height;

            if (wallTexture.YScale is float yScale)
            {
                yScale = (sector.Ceil - sector.Floor) * yScale;

                return yScale <= 1f;
            }
            else
            {
                return textureHeight <= (sector.Ceil - sector.Floor);
            }

        }

        // TODO: Simplify
        private static (int Height, int Width, float XScale, float ScaledTextureHeight) CalculateScale(
            RenderableSector sector,
            RenderableWall wall,
            GameTextureInfo wallTexture)
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
                scaledTextureHeight = (textureHeight << 16) * yScale;
            }
            else
            {
                scaledTextureHeight = (sector.Ceil - sector.Floor) << 16;
            }

            return (textureHeight, textureWidth, xScale, scaledTextureHeight);
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
                (float floorZ_a, float ceilingZ_a) = CalculateZAtPoint(sector, wall.C2);
                (float floorZ_b, float ceilingZ_b) = CalculateZAtPoint(sector, wall.C1);

                (float p_floorZ_a, float p_ceilingZ_a) = CalculateZAtPoint(neighborSector, wall.C1);
                (float p_floorZ_b, float p_ceilingZ_b) = CalculateZAtPoint(neighborSector, wall.C2);

                (sectorHeight, ceilOffset, floorOffset) = CalculatePortalOffsets(floorZ_a, ceilingZ_a, p_floorZ_a, p_ceilingZ_a);

                renderLower = floorOffset != 0;
                renderUpper = ceilOffset != 0;
                basicWall = !(floorOffset == sectorHeight || sectorHeight == -ceilOffset);

                (sectorHeight, ceilOffset, floorOffset) = CalculatePortalOffsets(floorZ_b, ceilingZ_b, p_floorZ_b, p_ceilingZ_b);

                renderLower |= floorOffset != 0;
                renderUpper |= ceilOffset != 0;
                basicWall &= !(floorOffset == sectorHeight || sectorHeight == -ceilOffset);

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

        private static (float SectorHeight, float CeilingOffset, float FloorOffset) CalculatePortalOffsets(ReadOnlySpan<RenderableSector> sectors, RenderableWall wall)
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
