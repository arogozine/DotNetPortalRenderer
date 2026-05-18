using RenderingEngine.Models;
using RenderingEngine.Tooling;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        #region Shared Precalculated Vectors

        private Vector<float> pxV = default;
        private Vector<float> pyV = default;
        private Vector<float> pzV = default;
        private Vector<float> pSinV = default;
        private Vector<float> pCosV = default;

        [SkipLocalsInit]
        public void InitializeSharedVectors(PortalPlayerSnapshot player)
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

        [Flags]
        private enum XyOpts : byte
        {
            None = 0,
            FlipX = 1,
            FlipY = 2,
            SwapXY = 4,
            DoubleSize = 8
        }

        private static (int xOffset, int yOffset, XyOpts Opts) DetermineOffsets(
            TextureInfo textureInfo)
        {
            int textureWidth = textureInfo.Width;
            int textureHeight = textureInfo.Height;

            XyOpts xyOpts = XyOpts.None;
            int xOffset = textureInfo.XOffset;
            int yOffset = textureInfo.YOffset;

            bool doubleSize = textureInfo.XScale == 2 && textureInfo.YScale == 2;

            const TextureRenderingOptions mask = TextureRenderingOptions.FlipX | TextureRenderingOptions.FlipY | TextureRenderingOptions.SwapXY;

            switch (textureInfo.RenderingOptions & mask)
            {
                case TextureRenderingOptions.FlipX:
                    xOffset = textureWidth - xOffset;
                    xyOpts = XyOpts.FlipX;
                    break;
                case TextureRenderingOptions.FlipX | TextureRenderingOptions.FlipY:
                    yOffset = textureHeight - yOffset;
                    break;
                case TextureRenderingOptions.FlipX | TextureRenderingOptions.SwapXY:
                    xyOpts = XyOpts.SwapXY | XyOpts.FlipY;
                    break;
                case TextureRenderingOptions.FlipY:
                    xyOpts = XyOpts.FlipY;
                    break;
                case TextureRenderingOptions.FlipY | TextureRenderingOptions.SwapXY:
                    xOffset = textureWidth - xOffset;
                    xyOpts = XyOpts.SwapXY | XyOpts.FlipX;
                    break;
                case TextureRenderingOptions.SwapXY:
                    yOffset = textureHeight - yOffset;
                    xyOpts = XyOpts.SwapXY | XyOpts.FlipY;
                    break;
                case TextureRenderingOptions.FlipX | TextureRenderingOptions.FlipY | TextureRenderingOptions.SwapXY:
                    xyOpts = XyOpts.SwapXY;
                    break;
                case TextureRenderingOptions.None:
                    xOffset = textureWidth - xOffset;
                    yOffset = textureHeight - yOffset;
                    break;
                default:
                    throw new NotImplementedException();
            }

            if (doubleSize)
            {
                xOffset <<= 1;
                yOffset <<= 1;
                xyOpts |= XyOpts.DoubleSize;
            }

            return (xOffset, yOffset, xyOpts);
        }

        [SkipLocalsInit]
        private unsafe void RenderCeilingVector(
            PortalPlayerSnapshot player,
            Sector sector)
        {
            ReadOnlySpan<RenderColumnStatus> statusSpan = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            Span<int> ceilingStart = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            ReadOnlySpan<int> wallStartSloped = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped);
            ReadOnlySpan<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);

            bool rotated = sector.Settings.HasFlag(MapSectorSettings.RotateCeiling);

            int width = PixelWidth;

            float pz = player.Z;
            float yCeil = sector.Ceil - pz;

            TextureInfo ceilingTexture = sector.CeilTexture;

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

            ref uint ceilingTexturePtr = ref ceilingTexture.Texture.GetBinaryRef<uint>(sector.CeilingShade, transform);
            uint* screenPtr = (uint*)buffer;

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            int length = sectorToX - sectorFromX + 1;
            Span<int> wallStartClamped = TempBuffer<int>.GetBuffer(length);
            Span<ushort> repeatedCount = TempBuffer<ushort>.GetBuffer(length);
            repeatedCount.Fill((ushort)length);

            for (int x = sectorFromX; x <= sectorToX; x++)
            {
                RenderColumnStatus columnStatus = statusSpan[x];

                if (!columnStatus.CeilingRenderable)
                {
                    repeatedCount[x - sectorFromX] = 0;
                    continue;
                }

                int floorEndY = floorEnd[x];
                int ceilingStartY = ceilingStart[x];

                wallStartClamped[x - sectorFromX] = SharedHelpers.Clamp(wallStartSloped[x], ceilingStartY, floorEndY);
            }

            _ = SharedHelpers.PopulateRepeatedValuesInPlace(repeatedCount);

            fixed (uint* texturePtr = &ceilingTexturePtr)
            {
                RenderFloorOrCeilingColumn(repeatedCount, screenPtr, texturePtr, sectorFromX, sectorToX, wallStartClamped, ceilingStart[sectorFromX..], width,
                    yCeil, yOffset, xOffset, textureWidth,
                    textureHeightMask, textureWidthMask, rotated, rSinV, rCosV, alignXV, alignYV, xyOpts, sector, sector.Settings.HasFlag(MapSectorSettings.SlopeCeiling) ? false : null);
            }
        }

        [SkipLocalsInit]
        public unsafe void RenderFloorVector(PortalPlayerSnapshot player, Sector sector)
        {
            Span<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            Span<int> wallEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped);
            Span<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);
            ReadOnlySpan<int> ceilingStart = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);

            bool rotated = sector.Settings.HasFlag(MapSectorSettings.RotateFloor);
            TextureInfo floorTexture = sector.FloorTexture;
            int width = PixelWidth;

            float yfloor = sector.Floor - player.Z;

            int textureWidth = floorTexture.Width;

            int textureHeightMask = floorTexture.Height - 1;
            int textureWidthMask = floorTexture.Width - 1;

            var firstWall = sector.Walls[0];
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

            ref uint floorTexturePtr = ref floorTexture.Texture.GetBinaryRef<uint>(sector.CeilingShade, transform);
            uint* screenPtr = (uint*)buffer;

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

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            int length = sectorToX - sectorFromX + 1;
            Span<int> wallEndClamped = TempBuffer<int>.GetBuffer(length);
            Span<ushort> repeatedCount = TempBuffer<ushort>.GetBuffer(length);
            repeatedCount.Fill((ushort)length);

            for (int x = sectorFromX; x <= sectorToX; x++)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.FloorRenderable)
                {
                    repeatedCount[x - sectorFromX] = 0;
                    continue;
                }

                int floorEndY = floorEnd[x];
                int ceilingStartY = ceilingStart[x];

                wallEndClamped[x - sectorFromX] = SharedHelpers.Clamp(wallEnd[x], ceilingStartY, floorEndY);
            }

            _ = SharedHelpers.PopulateRepeatedValuesInPlace(repeatedCount);

            fixed (uint* texturePtr = &floorTexturePtr)
            {
                Sse.Prefetch2(texturePtr);

                RenderFloorOrCeilingColumn(repeatedCount, screenPtr, texturePtr, sectorFromX, sectorToX, floorEnd[sectorFromX..], wallEndClamped, width,
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
            Span<int> floorTo,
            Span<int> floorFrom,
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
            Sector sector,
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
            // For slopes
            Vector3 planePoint, planeNormal;
            Vector<float> nX, nY, nZ, pX, pY, pZ, dir_z;
            // Scalar
            float dir_z_scalar;
            float alignX = alignXV[0];
            float alignY = alignYV[0];
            float rSin = rSinV[0];
            float rCos = rCosV[0];
            float pSin = pSinV[0];
            float pCos = pCosV[0];
            float px = pxV[0];
            float py = pyV[0];
            Vector3 linePoint = new(0f, 0f, pzV[0]);

            int halfHeight = PixelHeight / 2;

            float* xMapPosMultiplierCachePtr = memoryPool.GetBucketPtr<float>(MemoryPoolBucket.XMapPosMultiplierCache);
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
                    (int min_t, int max_t, int min_b, int max_b) = CalculateLaneTopBottoms(x - sectorFrom, floorFrom, floorTo);

                    if (min_b > max_t + 16)
                    {
                        RenderLine(x, floorTo[(x - sectorFrom)..], floorFrom[(x - sectorFrom)..],
                            min_t, max_t, min_b, max_b);

                        x += Vector<int>.Count;
                        count -= (ushort)Vector<int>.Count;
                        continue;
                    }
                }

                while (count-- > 0)
                {
                    int floorFromY = floorFrom[x - sectorFrom];
                    int floorToY = floorTo[x - sectorFrom];

                    RenderColumn(floorToY, floorFromY, x, *(xMapPosMultiplierCachePtr + x));
                    x++;
                }
            }

            return;

            unsafe void RenderLine(
                int x,
                ReadOnlySpan<int> to, ReadOnlySpan<int> from,
                int min_t, int max_t, int min_b, int max_b
                )
            {
                Vector<float> xMapPosMultiplierCacheV = Vector.Load(xMapPosMultiplierCachePtr + x);

                // render tops where there is no shared window
                if (min_t != max_t)
                {
                    RenderColumnAngleTop(min_t, max_t, from, x);
                }

                for (int y = max_t, screenIndex = y * width + x; y <= min_b; y++, screenIndex += width)
                {
                    Vector<float> incramentVector = Vector.Create(*(incrCachePtr + y));

                    uint* screenTexPtr = screenPtr + screenIndex;

                    Vector<int> textureIndex = GetXyFromScreenSpace(incramentVector, xMapPosMultiplierCacheV);

                    if (Avx2.IsSupported && Vector<int>.Count == Vector256<int>.Count)
                    {
                        Vector256<uint> gathered = Avx2.GatherVector256(texturePtr, textureIndex.AsVector256(), scale: sizeof(int));

                        gathered.Store(screenTexPtr);
                    }
                    else
                    {
                        for (int i = 0; i < Vector<int>.Count; i++)
                        {
                            screenTexPtr[i] = texturePtr[textureIndex[i]];
                        }
                    }
                }
                
                // render bottoms where there is no shared window
                if (min_b != max_b)
                {
                    RenderColumnAngleBottom(min_b, max_b, to, x);
                }
            }

            unsafe void RenderColumnAngleBottom(
                int floorFromY,
                int floorToY,
                ReadOnlySpan<int> to,
                int xStart)
            {
                uint* screenTexPtr = screenPtr + floorFromY * width + xStart;
                float* xMapPosMult = xMapPosMultiplierCachePtr + xStart;

                float* incr = incrCachePtr + floorFromY;

                for (int y = floorFromY; y < floorToY; y++)
                {
                    for (int i = 0; i < Vector<uint>.Count; i++)
                    {
                        if (to[i] > y)
                        {
                            float xMult = *(xMapPosMult + i);
                            int textureIndex = GetXyFromScreenSpaceScalar(*incr, xMult);
                            screenTexPtr[i] = texturePtr[textureIndex];
                        }
                    }

                    screenTexPtr += width;
                    incr++;
                }
            }

            unsafe void RenderColumnAngleTop(
                int min_t,
                int max_t,
                ReadOnlySpan<int> from,
                int xStart)
            {
                uint* screenTexPtr = screenPtr + min_t * width + xStart;
                float* xMapPosMult = xMapPosMultiplierCachePtr + xStart;

                float* incr = incrCachePtr + min_t;

                for (int y = min_t; y < max_t; y++)
                {
                    for (int i = 0; i < Vector<uint>.Count; i++)
                    {
                        if (from[i] >= y)
                        {
                            continue;
                        }

                        float xMult = *(xMapPosMult + i);
                        int textureIndex = GetXyFromScreenSpaceScalar(*incr, xMult);
                        screenTexPtr[i] = texturePtr[textureIndex];
                    }

                    screenTexPtr += width;
                    incr++;
                }
            }

            unsafe void RenderColumn(
                int floorToY, int floorFromY, int x, float xMapPosMultiplier)
            {
                int screenIndex = floorFromY * width + x;

                uint* screenTexPtr = screenPtr + screenIndex;
                Vector<float> xMapPosMultiplierV = Vector.Create(xMapPosMultiplier);

                int columnHeight = floorToY - floorFromY;
                int rem = columnHeight & (Vector<int>.Count - 1);

                Vector<float> incramentVector = Vector.Load(incrCachePtr + floorFromY);

                if (columnHeight != rem)
                {
                    floorToY -= rem;

                    uint* toScalePtr = screenPtr + floorToY * width + x;

                    uint* cur = screenTexPtr;

                    while (cur != toScalePtr)
                    {
                        Vector<int> textureIndex = GetXyFromScreenSpace(incramentVector, xMapPosMultiplierV);

                        if (Avx2.IsSupported && Vector<int>.Count == Vector256<int>.Count)
                        {
                            Vector256<uint> gathered = Avx2.GatherVector256(texturePtr, textureIndex.AsVector256(), scale: sizeof(uint));

                            for (int i = 0; i < Vector<int>.Count; i++)
                            {
                                *cur = gathered[i];
                                cur += width;
                            }
                        }
                        else
                        {
                            for (int i = 0; i < Vector<int>.Count; i++)
                            {
                                *cur = texturePtr[textureIndex[i]];
                                cur += width;
                            }
                        }

                        floorFromY += Vector<float>.Count;
                        incramentVector = Vector.Load(incrCachePtr + floorFromY);
                    }

                    screenTexPtr = cur;
                }

                for (int i = 0; i < rem; i++)
                {
                    int textureIndex = GetXyFromScreenSpaceScalar(incramentVector[i], xMapPosMultiplier);

                    *screenTexPtr = texturePtr[textureIndex];
                    screenTexPtr += width;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            Vector<int> GetXyFromScreenSpace(
                Vector<float> incramentVector,
                Vector<float> xMapPosMultiplierV
            )
            {
                Vector<float> yMapPosR = cameraPositionV * incramentVector;
                Vector<float> xMapPosR = yMapPosR * xMapPosMultiplierV;

                if (slopeFloor is not null)
                {
                    // direction vector
                    var dir_x = - xMapPosR;
                    var dir_y = - yMapPosR;

                    // Vectorized intersection for the whole vector lane
                    MathFormulas.FindIntersectionVectorZero(nX, nY, nZ, pX, pY, pZ, pzV,
                        dir_x, dir_y, dir_z,
                        out xMapPosR, out yMapPosR);
                }

                (Vector<float> xMapPos, Vector<float> yMapPos) = SharedHelpers.RotateVertexBack(xMapPosR, yMapPosR, pSinV, pCosV, pxV, pyV);

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
            int GetXyFromScreenSpaceScalar(float incrament, float xMapPosMultiplier)
            {
                float yMapPosR = cameraPosition * incrament;
                float xMapPosR = yMapPosR * xMapPosMultiplier;

                if (slopeFloor is not null)
                {
                    Vector3 lineDir = new(-xMapPosR, -yMapPosR, dir_z_scalar);

                    MathFormulas.FindIntersection(planePoint, planeNormal, linePoint, lineDir, out Vector3 intersection);
                    xMapPosR = intersection.X;
                    yMapPosR = intersection.Y;
                }

                (float xMapPos, float yMapPos) = SharedHelpers.RotateVertexBack(xMapPosR, yMapPosR, pSin, pCos, px, py);

                if (rotated)
                {
                    xMapPos -= alignX;
                    yMapPos -= alignY;

                    float xMapPosSR = MathF.FusedMultiplyAdd(xMapPos, rCos, -yMapPos * rSin);
                    float yMapPosSR = MathF.FusedMultiplyAdd(xMapPos, rSin, yMapPos * rCos);

                    xMapPos = xMapPosSR;
                    yMapPos = yMapPosSR;
                }

                int _y1 = float.ConvertToIntegerNative<int>(yMapPos);
                int _x1 = float.ConvertToIntegerNative<int>(xMapPos);

                if (xyOpts.HasFlag(XyOpts.DoubleSize))
                {
                    _y1 >>= 1;
                    _x1 >>= 1;
                }

                _y1 = (_y1 + yOffset) & textureHeightMask;
                _x1 = (_x1 + xOffset) & textureWidthMask;

                return _y1 * textureWidth + _x1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void CreateSlopeVectors()
            {
                if (slopeFloor is bool slopeFloorBoolean)
                {
                    (planePoint, planeNormal) = slopeFloorBoolean ? MathFormulas.CalculatePlaneNormalFloor(sector)
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

                    dir_z_scalar = dir_z[0];
                }
                else
                {
                    Unsafe.SkipInit(out planePoint);
                    Unsafe.SkipInit(out planeNormal);
                    Unsafe.SkipInit(out nX);
                    Unsafe.SkipInit(out nY);
                    Unsafe.SkipInit(out nZ);
                    Unsafe.SkipInit(out pX);
                    Unsafe.SkipInit(out pY);
                    Unsafe.SkipInit(out pZ);
                    Unsafe.SkipInit(out dir_z);
                    Unsafe.SkipInit(out dir_z_scalar);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int min_t, int max_t, int min_b, int max_b) CalculateLaneTopBottoms(int x, Span<int> from, Span<int> to)
        {
            if (Vector<int>.Count == 8)
            {
                Vector256<int> fromV = Vector256.LoadUnsafe(ref from[x]);
                Vector256<int> toV = Vector256.LoadUnsafe(ref to[x]);

                (int min_t, int max_t) = GetMinMaxValue(fromV);
                (int min_b, int max_b) = GetMinMaxValue(toV);

                return (min_t, max_t, min_b, max_b);
            }
            else if (Vector<int>.Count == 4)
            {
                Vector128<int> fromV = Vector128.LoadUnsafe(ref from[x]);
                Vector128<int> toV = Vector128.LoadUnsafe(ref to[x]);

                (int min_t, int max_t) = GetMinMaxValue(fromV);
                (int min_b, int max_b) = GetMinMaxValue(toV);

                return (min_t, max_t, min_b, max_b);
            }
            else
            {

                from = from[x..];
                to = to[x..];

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

                return (min_t, max_t, min_b, max_b);
            }
        }

        internal static (float FloorZ, float CeilingZ) CalculateZAtPoint(Sector sector, Point point)
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
            Point pointA = firstWall.R1;
            Point pointB = firstWall.R2;

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
