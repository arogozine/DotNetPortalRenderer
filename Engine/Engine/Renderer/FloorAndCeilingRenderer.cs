using RenderingEngine.Tooling;
using SoftwareRendererModels;
using System.Numerics;
using System.Runtime.Intrinsics;
using Tooling;

namespace RenderingEngine.Engine
{
    internal partial class PortalRenderer
    {
        #region Shared Precalculated Vectors

        private Vector<float> pxV;
        private Vector<float> pyV;
        private Vector<float> pzV;
        private Vector<float> pSinV;
        private Vector<float> pCosV;

        [SkipLocalsInit]
        private void InitializeSharedVectors(PortalPlayerSnapshot player)
        {
            float px = player.X;
            float py = player.Y;
            float pz = player.Z;
            float pSin = player.Sin;
            float pCos = player.Cos;

            pxV = Vector.Create(px);
            pyV = Vector.Create(py);
            pzV = Vector.Create(pz);

            pSinV = Vector.Create(pSin);
            pCosV = Vector.Create(pCos);
        }

        #endregion

        protected abstract (int xOffset, int yOffset, XyOpts Opts) DetermineOffsets(
            GameTextureInfo textureInfo);

        [SkipLocalsInit]
        private unsafe void RenderCeilingVector(
            PortalPlayerSnapshot player,
            RenderableSector sector,
            int sectorFromX, int sectorToX,
            bool temp1)
        {
            RenderColumnStatus* statusPtr = memoryPool.GetBucketPtr<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            int* ceilingStart = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.CeilingStart);
            int* wallStartSloped = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.WallStartClamped);
            int* floorEnd = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.FloorEnd);
            int* wallStartClampedPtr = memoryPool.GetBucketPtr<int>(temp1 ? MemoryPoolBucket.Temp : MemoryPoolBucket.Temp3);
            wallStartClampedPtr += sectorFromX;

            bool rotated = sector.Settings.HasFlag(MapSectorSettings.RotateCeiling);

            int width = PixelWidth;

            float pz = player.Z;
            float yCeil = sector.Ceil - pz;

            GameTextureInfo ceilingTexture = sector.CeilTexture;

            int textureWidth = ceilingTexture.Width;
            int textureHeightMask = ceilingTexture.Height - 1;
            int textureWidthMask = ceilingTexture.Width - 1;

            (int xOffset, int yOffset, XyOpts xyOpts) = DetermineOffsets(ceilingTexture);

            Unsafe.SkipInit(out Vector<float> rSinV);
            Unsafe.SkipInit(out Vector<float> rCosV);
            Unsafe.SkipInit(out Vector<float> alignXV);
            Unsafe.SkipInit(out Vector<float> alignYV);

            if (rotated)
            {
                (float rSin, float rCos) = MathF.SinCos(sector.RotationCeiling!.Value);
                rSinV = Vector.Create(rSin);
                rCosV = Vector.Create(rCos);

                (float aX, float aY) = sector.Walls[0].PointA;

                alignXV = Vector.Create(aX);
                alignYV = Vector.Create(aY);
            }

            var transform = TextureTransform.Normal;

            if (xyOpts.HasFlag(XyOpts.FlipX))
            {
                transform |= TextureTransform.FlippedX;
                xyOpts ^= XyOpts.FlipX;
            }

            if (xyOpts.HasFlag(XyOpts.FlipY))
            {
                transform |= TextureTransform.FlippedY;
                xyOpts ^= XyOpts.FlipY;
            }

            if (xyOpts.HasFlag(XyOpts.SwapXY))
            {
                transform |= TextureTransform.Rotated;
                xyOpts ^= XyOpts.SwapXY;
                (xOffset, yOffset) = (yOffset, xOffset);
                textureWidth = ceilingTexture.Height;
                (textureHeightMask, textureWidthMask) = (textureWidthMask, textureHeightMask);
            }

            ref uint ceilingTexturePtr = ref ceilingTexture.Texture.GetBinaryRef<uint>(sector.CeilTexture.Palette, sector.CeilingShade, transform);
            uint* screenPtr = (uint*)Buffer;

            Span<ushort> repeatedCount = memoryPool.GetBucket<ushort>(temp1 ? MemoryPoolBucket.Temp2 : MemoryPoolBucket.Temp4)[sectorFromX..(sectorToX + 1)];
            repeatedCount.Fill((ushort)repeatedCount.Length);

            for (int x = sectorFromX; x <= sectorToX; x++)
            {
                RenderColumnStatus columnStatus = statusPtr[x];

                if (!columnStatus.CeilingRenderable)
                {
                    repeatedCount[x - sectorFromX] = 0;
                    continue;
                }

                int floorEndY = floorEnd[x];
                int ceilingStartY = ceilingStart[x];

                wallStartClampedPtr[x - sectorFromX] = SharedHelpers.Clamp(wallStartSloped[x], ceilingStartY, floorEndY);
            }

            _ = SharedHelpers.PopulateRepeatedValuesInPlace(repeatedCount);

            fixed (uint* texturePtr = &ceilingTexturePtr)
            {
                RenderFloorOrCeilingColumn(repeatedCount, screenPtr, texturePtr, sectorFromX, sectorToX, wallStartClampedPtr, ceilingStart + sectorFromX, width,
                    yCeil, yOffset, xOffset, textureWidth,
                    textureHeightMask, textureWidthMask, rotated, rSinV, rCosV, alignXV, alignYV, xyOpts, sector, sector.Settings.HasFlag(MapSectorSettings.SlopeCeiling) ? false : null);
            }
        }

        [SkipLocalsInit]
        public unsafe void RenderFloorVector(
            PortalPlayerSnapshot player,
            RenderableSector sector,
            int sectorFromX, int sectorToX,
            bool temp1)
        {
            RenderColumnStatus* statusPtr = memoryPool.GetBucketPtr<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            int* wallEnd = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.WallEndClamped);
            int* floorEnd = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.FloorEnd);
            int* ceilingStart = memoryPool.GetBucketPtr<int>(MemoryPoolBucket.CeilingStart);
            // AI Assisted: floor rendering uses Temp3/Temp4 (rather than Temp/Temp2, used by ceiling
            // rendering) so it can run concurrently with ceiling rendering without racing on the same buffer
            int* wallEndClampedPtr = memoryPool.GetBucketPtr<int>(temp1 ? MemoryPoolBucket.Temp : MemoryPoolBucket.Temp3);
            wallEndClampedPtr += sectorFromX;

            bool rotated = sector.Settings.HasFlag(MapSectorSettings.RotateFloor);
            GameTextureInfo floorTexture = sector.FloorTexture;
            int width = PixelWidth;

            float yfloor = sector.Floor - player.Z;

            int textureWidth = floorTexture.Width;

            int textureHeightMask = floorTexture.Height - 1;
            int textureWidthMask = floorTexture.Width - 1;

            RenderableWall firstWall = sector.Walls[0];
            (float x1, float y1) = firstWall.PointA;
            (float x2, float y2) = firstWall.PointB;

            (int xOffset, int yOffset, XyOpts xyOpts) = DetermineOffsets(floorTexture);

            var transform = TextureTransform.Normal;

            if (xyOpts.HasFlag(XyOpts.FlipX))
            {
                transform |= TextureTransform.FlippedX;
                xyOpts ^= XyOpts.FlipX;
            }

            if (xyOpts.HasFlag(XyOpts.FlipY))
            {
                transform |= TextureTransform.FlippedY;
                xyOpts ^= XyOpts.FlipY;
            }

            if (xyOpts.HasFlag(XyOpts.SwapXY))
            {
                transform |= TextureTransform.Rotated;
                xyOpts ^= XyOpts.SwapXY;
                (xOffset, yOffset) = (yOffset, xOffset);
                textureWidth = floorTexture.Height;
                (textureHeightMask, textureWidthMask) = (textureWidthMask, textureHeightMask);
            }

            ref uint floorTexturePtr = ref floorTexture.Texture.GetBinaryRef<uint>(sector.FloorTexture.Palette, sector.FloorShade, transform);
            uint* screenPtr = (uint*)Buffer;

            Unsafe.SkipInit(out Vector<float> rSinV);
            Unsafe.SkipInit(out Vector<float> rCosV);
            Unsafe.SkipInit(out Vector<float> alignWallXV);
            Unsafe.SkipInit(out Vector<float> alignWallYV);

            if (rotated)
            {
                if (x2 * y1 > y2 * x1)
                {
                    yOffset = -yOffset;
                }

                float angle = MathFormulas.ClampAngle(sector.RotationFloor!.Value);

                if (angle > MathF.PI)
                {
                    yOffset = -yOffset; // likely incorrect?
                }

                (float rSin, float rCos) = MathF.SinCos(angle);

                rSinV = Vector.Create(rSin);
                rCosV = Vector.Create(rCos);
                alignWallXV = Vector.Create(x1);
                alignWallYV = Vector.Create(y1);
            }

            Span<ushort> repeatedCount = memoryPool.GetBucket<ushort>(temp1 ? MemoryPoolBucket.Temp2 : MemoryPoolBucket.Temp4)[sectorFromX..(sectorToX + 1)];
            repeatedCount.Fill((ushort)repeatedCount.Length);

            for (int x = sectorFromX; x <= sectorToX; x++)
            {
                RenderColumnStatus columnStatus = statusPtr[x];

                if (!columnStatus.FloorRenderable)
                {
                    repeatedCount[x - sectorFromX] = 0;
                    continue;
                }

                int floorEndY = floorEnd[x];
                int ceilingStartY = ceilingStart[x];

                wallEndClampedPtr[x - sectorFromX] = SharedHelpers.Clamp(wallEnd[x], ceilingStartY, floorEndY);
            }

            _ = SharedHelpers.PopulateRepeatedValuesInPlace(repeatedCount);

            fixed (uint* texturePtr = &floorTexturePtr)
            {
                RenderFloorOrCeilingColumn(repeatedCount, screenPtr, texturePtr, sectorFromX, sectorToX, floorEnd + sectorFromX, wallEndClampedPtr, width,
                    yfloor, yOffset, xOffset, textureWidth,
                    textureHeightMask, textureWidthMask, rotated, rSinV, rCosV, alignWallXV, alignWallYV, xyOpts, sector, sector.Settings.HasFlag(MapSectorSettings.SlopeFloor) ? true : null);
            }
        }

        [SkipLocalsInit]
        private unsafe void RenderFloorOrCeilingColumn(
            Span<ushort> repeatedCount,
            uint* screenPtr,
            uint* texturePtr,
            int sectorFrom, int sectorTo,
            int* floorTo,
            int* floorFrom,
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
            XyOpts xyOpts,
            RenderableSector sector,
            bool? slopeFloor
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
            // Player Position
            Vector<float> pSinV = this.pSinV;
            Vector<float> pCosV = this.pCosV;
            Vector<float> pxV = this.pxV;
            Vector<float> pyV = this.pyV;
            Vector<float> pzV = this.pzV;
            // X Map Position Multiplier
            Vector<int> widthDiv2V = Vector.Create(width >> 1) - Vector.CreateSequence(0, 1);
            Vector<float> xPosIncrV = Vector.Create(1f / (width * -EngineConstants.HeightToWidthRatio));
            // For slopes
            Vector<float> nX, nY, nZ, pX, pY, pZ, dir_z;

            float* incrCachePtr = memoryPool.GetBucketPtr<float>(MemoryPoolBucket.CameraHeightToMapYPos);

            CreateSlopeVectors();

            for (int x = sectorFrom; x <= sectorTo;)
            {
                ushort count = repeatedCount[x - sectorFrom];

                if (count == 0)
                {
                    x++;
                    continue;
                }

                // Attempt horizontal rendering
                if (count >= Vector<int>.Count)
                {
                    (int min_t, int max_t, int min_b, int max_b) = CalculateLaneTopBottoms(x - sectorFrom, floorFrom, floorTo,
                        out Vector<int> from, out Vector<int> to);

                    if (min_b > max_t)
                    {
                        RenderLine(x, to, from,
                            min_t, max_t, min_b, max_b);

                        x += Vector<int>.Count;

                        continue;
                    }
                }

                while (count-- > 0)
                {
                    int floorFromY = floorFrom[x - sectorFrom];
                    int floorToY = floorTo[x - sectorFrom];

                    int widthDiv2 = width >> 1;
                    float xMapPosMultiplierCache = (widthDiv2 - x) * xPosIncrV[0];

                    RenderColumn(floorToY, floorFromY, x, xMapPosMultiplierCache);
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
                Vector<float> diff = Vector.ConvertToSingle(widthDiv2V - Vector.Create(x));
                Vector<float> xMapPosMultiplierCacheV = diff * xPosIncrV;

                // render tops where there is no shared window
                if (min_t != max_t)
                {
                    RenderColumnAngleTop(min_t, max_t, from, x, xMapPosMultiplierCacheV);
                }

                for (int y = max_t, screenIndex = y * width + x; y <= min_b; y++, screenIndex += width)
                {
                    uint* screenTexPtr = screenPtr + screenIndex;

                    Vector<int> textureIndex = GetXyFromScreenSpace(Vector.Create(*(incrCachePtr + y)), xMapPosMultiplierCacheV);
                    Vector<uint> gathered = Vector.Gather(texturePtr, textureIndex);
                    gathered.Store(screenTexPtr);
                }

                // render bottoms where there is no shared window
                if (min_b != max_b)
                {
                    RenderColumnAngleBottom(min_b, max_b, to, x, xMapPosMultiplierCacheV);
                }
            }

            void RenderColumnAngleBottom(
                int floorFromY,
                int floorToY,
                Vector<int> toV,
                int xStart,
                Vector<float> xMapPosMultV)
            {
                uint* screenTexPtr = screenPtr + floorFromY * width + xStart;

                for (int y = floorFromY; y < floorToY; y++)
                {
                    Vector<float> incrementVector = Vector.Create(*(incrCachePtr + y));
                    Vector<int> textureIndexV = GetXyFromScreenSpace(incrementVector, xMapPosMultV);

                    Vector<int> yV = Vector.Create(y);
                    Vector<int> mask = Vector.GreaterThan(toV, yV);
                    Vector<int> gathered = Vector.Gather((int*)texturePtr, textureIndexV);
                    Vector.MaskStore((int*)screenTexPtr, mask, gathered);

                    screenTexPtr += width;
                }
            }

            void RenderColumnAngleTop(
                int min_t,
                int max_t,
                Vector<int> fromV,
                int xStart,
                Vector<float> xMapPosMultV)
            {
                uint* screenTexPtr = screenPtr + min_t * width + xStart;

                for (int y = min_t; y < max_t; y++)
                {
                    Vector<float> incrementVector = Vector.Create(*(incrCachePtr + y));
                    Vector<int> textureIndexV = GetXyFromScreenSpace(incrementVector, xMapPosMultV);

                    Vector<int> yV = Vector.Create(y);
                    Vector<int> mask = Vector.LessThan(fromV, yV);
                    Vector<int> gathered = Vector.Gather((int*)texturePtr, textureIndexV);
                    Vector.MaskStore((int*)screenTexPtr, mask, gathered);

                    screenTexPtr += width;
                }
            }

            void RenderColumn(
                int floorToY, int floorFromY, int x, float xMapPosMultiplier)
            {
                int screenIndex = floorFromY * width + x;

                uint* screenTexPtr = screenPtr + screenIndex;
                Vector<float> xMapPosMultiplierV = Vector.Create(xMapPosMultiplier);

                int columnHeight = floorToY - floorFromY;
                int rem = columnHeight & (Vector<int>.Count - 1);

                Vector<float> incrementVector = Vector.Load(incrCachePtr + floorFromY);

                if (columnHeight != rem)
                {
                    floorToY -= rem;

                    uint* toScalePtr = screenPtr + floorToY * width + x;

                    uint* cur = screenTexPtr;

                    while (cur != toScalePtr)
                    {
                        Vector<int> textureIndex = GetXyFromScreenSpace(incrementVector, xMapPosMultiplierV);

                        Vector<uint> gathered = Vector.Gather(texturePtr, textureIndex);

                        for (int i = 0; i < Vector<int>.Count; i++)
                        {
                            *cur = gathered[i];
                            cur += width;
                        }

                        floorFromY += Vector<float>.Count;
                        incrementVector = Vector.Load(incrCachePtr + floorFromY);
                    }

                    screenTexPtr = cur;
                }

                if (rem != 0)
                {
                    Vector<int> textureIndexV = GetXyFromScreenSpace(incrementVector, xMapPosMultiplierV);

                    for (int i = 0; i < rem; i++)
                    {
                        int textureIndex = textureIndexV[i];
                        *screenTexPtr = texturePtr[textureIndex];
                        screenTexPtr += width;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            Vector<int> GetXyFromScreenSpace(
                Vector<float> incrementVector,
                Vector<float> xMapPosMultiplierV
            )
            {
                Vector<float> yMapPos = cameraPositionV * incrementVector;
                Vector<float> xMapPos = yMapPos * xMapPosMultiplierV;

                if (slopeFloor is not null)
                {
                    // direction vector
                    Vector<float> dir_x = -xMapPos;
                    Vector<float> dir_y = -yMapPos;

                    // Vectorized intersection for the whole vector lane
                    MathFormulas.FindIntersectionVectorZero(nX, nY, nZ, pX, pY, pZ, pzV,
                        dir_x, dir_y, dir_z,
                        out xMapPos, out yMapPos);
                }

                (xMapPos, yMapPos) = SharedHelpers.RotateVertexBack(xMapPos, yMapPos, pSinV, pCosV, pxV, pyV);

                if (rotated)
                {
                    xMapPos -= alignXV;
                    yMapPos -= alignYV;

                    Vector<float> xMapPosSR = Vector.FusedMultiplyAdd(xMapPos, rCosV, -yMapPos * rSinV);
                    Vector<float> yMapPosSR = Vector.FusedMultiplyAdd(xMapPos, rSinV, yMapPos * rCosV);

                    xMapPos = xMapPosSR;
                    yMapPos = yMapPosSR;
                }

                Vector<int> _y1 = Vector.ConvertToInt32Native(yMapPos);
                Vector<int> _x1 = Vector.ConvertToInt32Native(xMapPos);

                if (xyOpts.HasFlag(XyOpts.DoubleSize))
                {
                    _y1 >>= 1;
                    _x1 >>= 1;
                }

                _y1 = (_y1 + yOffSetV) & textureHeightMaskV;
                _x1 = (_x1 + xOffSetV) & textureWidthMaskV;

                return _y1 * textureWidthV + _x1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void CreateSlopeVectors()
            {
                if (slopeFloor is { } slopeFloorBoolean)
                {
                    (var planePoint, var planeNormal) = slopeFloorBoolean ? MathFormulas.CalculatePlaneNormalFloor(sector)
                        : MathFormulas.CalculatePlaneNormalCeil(sector);

                    // Convert scalar plane data into vectors
                    nX = Vector.Create(planeNormal.X);
                    nY = Vector.Create(planeNormal.Y);
                    nZ = Vector.Create(planeNormal.Z);
                    pX = Vector.Create(planePoint.X);
                    pY = Vector.Create(planePoint.Y);
                    pZ = Vector.Create(planePoint.Z);

                    if (cameraPosition == 0)
                    {
                        dir_z = pzV;
                        cameraPositionV = -pzV;
                    }
                    else
                    {
                        dir_z = pzV - Vector.Create<float>(slopeFloorBoolean ? sector.Floor : sector.Ceil);
                    }
                }
                else
                {
                    Unsafe.SkipInit(out nX);
                    Unsafe.SkipInit(out nY);
                    Unsafe.SkipInit(out nZ);
                    Unsafe.SkipInit(out pX);
                    Unsafe.SkipInit(out pY);
                    Unsafe.SkipInit(out pZ);
                    Unsafe.SkipInit(out dir_z);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe (int min_t, int max_t, int min_b, int max_b) CalculateLaneTopBottoms(
            int x, int* from, int* to, out Vector<int> fromVec, out Vector<int> toVec)
        {
            from += x;
            to += x;

            if (Vector<int>.Count == Vector256<int>.Count)
            {
                Vector256<int> fromV = Vector256.Load(from);
                Vector256<int> toV = Vector256.Load(to);

                (int min_t, int max_t) = MathFormulas.GetMinMaxValue(fromV);
                (int min_b, int max_b) = MathFormulas.GetMinMaxValue(toV);

                fromVec = fromV.AsVector();
                toVec = toV.AsVector();

                return (min_t, max_t, min_b, max_b);
            }
            else if (Vector<int>.Count == Vector128<int>.Count)
            {
                Vector128<int> fromV = Vector128.Load(from);
                Vector128<int> toV = Vector128.Load(to);

                (int min_t, int max_t) = MathFormulas.GetMinMaxValue(fromV);
                (int min_b, int max_b) = MathFormulas.GetMinMaxValue(toV);

                fromVec = fromV.AsVector();
                toVec = toV.AsVector();

                return (min_t, max_t, min_b, max_b);
            }
            else
            {
                int min_t = int.MaxValue, max_t = int.MinValue;
                int min_b = int.MaxValue, max_b = int.MinValue;

                // compute per-lane tops/bottoms
                for (int i = 0; i < Vector<int>.Count; i++)
                {
                    int top = from[i];
                    min_t = MathFormulas.Min(min_t, top);
                    max_t = MathFormulas.Max(max_t, top);

                    int bottom = to[i];
                    min_b = MathFormulas.Min(min_b, bottom);
                    max_b = MathFormulas.Max(max_b, bottom);
                }

                fromVec = Vector.Load(from);
                toVec = Vector.Load(to);

                return (min_t, max_t, min_b, max_b);
            }
        }

        internal static (float FloorZ, float CeilingZ) CalculateZAtPoint(RenderableSector sector, Vector2 point)
        {
            float ceilZ = sector.Ceil;
            float floorZ = sector.Floor;
            float floorSlope = sector.FloorSlope ?? 0f;
            float ceilingSlope = sector.CeilingSlope ?? 0f;

            if (floorSlope == 0f && ceilingSlope == 0f)
            {
                return (floorZ, ceilZ);
            }

            // PointA and PointB of first line
            RenderableWall firstWall = sector.Walls[0];
            Vector2 pointA = firstWall.R1;
            Vector2 pointB = firstWall.R2;

            float dx = pointB.X - pointA.X;
            float dy = pointB.Y - pointA.Y;

            float distance = MathF.Sqrt(dx * dx + dy * dy);

            if (distance == 0f)
            {
                return (floorZ + ceilingSlope, ceilZ + floorSlope);
            }

            // compute signed perpendicular from the reference line
            (float x, float y) = point;
            float offset = dx * (y - pointA.Y) - dy * (x - pointA.X);

            if (sector.Settings.HasFlag(MapSectorSettings.SlopeCeiling))
            {
                ceilZ += (ceilingSlope * offset) / distance;
            }

            if (sector.Settings.HasFlag(MapSectorSettings.SlopeFloor))
            {
                floorZ += (floorSlope * offset) / distance;
            }

            return (floorZ, ceilZ);
        }

    }
}
