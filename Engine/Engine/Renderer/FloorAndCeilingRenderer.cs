using RenderingEngine.Models;
using RenderingEngine.Tooling;
using System.Numerics;

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

        [SkipLocalsInit]
        private void RenderCeilingVector(
            PortalPlayerSnapshot player,
            Sector sector)
        {
            ReadOnlySpan<RenderColumnStatus> statusSpan = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            ReadOnlySpan<int> ceilingStartSpan = memoryPool.GetBucket<int>(MemoryPoolBucket.CeilingStart);
            ReadOnlySpan<int> wallStartSpan = memoryPool.GetBucket<int>(MemoryPoolBucket.WallStartClamped);

            bool rotated = sector.Settings.HasFlag(MapSectorSettings.RotateCeiling);

            int width = PixelWidth;

            float pz = player.Z;
            float yCeil = sector.Ceil - pz;

            Vector<float> yCeilV = Vector.Create(yCeil);

            TextureInfo ceilingTexture = sector.CeilTexture;
            (bool swapXy, bool flipX, bool flipY, bool doubleSize) = GetFloorFlags(ceilingTexture);

            int textureWidth = ceilingTexture.Width;
            int textureHeightMask = ceilingTexture.Height - 1;
            int textureWidthMask = ceilingTexture.Width - 1;
            Vector<int> textureHeightMaskV = Vector.Create(textureHeightMask);
            Vector<int> textureWidthMaskV = Vector.Create(textureWidthMask);
            Vector<int> textureWidthV = Vector.Create(textureWidth);

            int xOffset = ceilingTexture.XOffset;
            int yOffset = ceilingTexture.YOffset;
            Vector<int> xOffSetV = Vector.Create(xOffset);
            Vector<int> yOffSetV = Vector.Create(yOffset);

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

            Span<float> xMapPosMultiplierCache = memoryPool.GetBucket<float>(MemoryPoolBucket.XMapPosMultiplierCache);
            ref uint ceilingTexturePtr = ref ceilingTexture.Texture.GetBinaryRef<uint>(false, sector.CeilingShade);
            ref uint screenPtr = ref GetScreenPtr<uint>();

            (int sectroFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            for (int x = sectroFromX; x <= sectorToX; x++)
            {
                RenderColumnStatus columnStatus = statusSpan[x];

                if (!columnStatus.CeilingRenderable)
                {
                    continue;
                }

                int ceilingStart = ceilingStartSpan[x];
                int wallStart = wallStartSpan[x];

                int screenIndex = ceilingStart * width + x;

                float xMapPosMultiplier = xMapPosMultiplierCache[x];

                RenderFloorOrCeilingColumn(ref screenPtr, ref ceilingTexturePtr, screenIndex, wallStart, ceilingStart, width,
                    x, yCeilV, xMapPosMultiplier, yOffSetV, xOffSetV, textureWidthV,
                    textureHeightMaskV, textureWidthMaskV, rotated, rSinV, rCosV, alignXV, alignYV, flipY, flipX, swapXy, doubleSize, sector, sector.Settings.HasFlag(MapSectorSettings.SlopeCeiling) ? false : null);
            }
        }

        [SkipLocalsInit]
        public void RenderFloorVector(PortalPlayerSnapshot player, Sector sector)
        {
            Span<RenderColumnStatus> status = memoryPool.GetBucket<RenderColumnStatus>(MemoryPoolBucket.RenderColumnStatus);
            Span<int> floorEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.FloorEnd);
            Span<int> wallEnd = memoryPool.GetBucket<int>(MemoryPoolBucket.WallEndClamped);
            Span<float> xMapPosMultiplierCache = memoryPool.GetBucket<float>(MemoryPoolBucket.XMapPosMultiplierCache);

            bool rotated = sector.Settings.HasFlag(MapSectorSettings.RotateFloor);
            TextureInfo floorTexture = sector.FloorTexture;
            int width = PixelWidth;

            float yfloor = sector.Floor - player.Z;
            (bool swapXy, bool flipX, bool flipY, bool doubleSize) = GetFloorFlags(floorTexture);

            ref uint floorTexturePtr = ref floorTexture.Texture.GetBinaryRef<uint>(false, sector.FloorShade);
            ref uint screenPtr = ref GetScreenPtr<uint>();

            Vector<float> yfloorV = Vector.Create(yfloor);

            int textureWidth = floorTexture.Width;
            int textureHeight = floorTexture.Height;

            int textureHeightMask = floorTexture.Height - 1;
            int textureWidthMask = floorTexture.Width - 1;

            Vector<int> textureHeightMaskV = Vector.Create(textureHeightMask);
            Vector<int> textureWidthMaskV = Vector.Create(textureWidthMask);
            Vector<int> textureWidthV = Vector.Create(textureWidth);

            var firstWall = sector.Walls[0];
            (float x1, float y1) = firstWall.PointA;
            (float x2, float y2) = firstWall.PointB;

            int xOffset = floorTexture.XOffset;
            int yOffset = floorTexture.YOffset;

            if (doubleSize)
            {
                xOffset <<= 1;
                yOffset <<= 1;
            }

            if (swapXy)
            {
                /*
                if (flipY)
                {
                    yOffset = -yOffset;
                }

                if (flipX)
                {
                    xOffset = -xOffset;
                }
                */
            }
            else
            {
                if (flipY)
                {
                    yOffset = textureHeight - yOffset;
                }

                flipY = !flipY;
                flipX = !flipX;
            }

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

            Vector<int> xOffSetV = Vector.Create(xOffset);
            Vector<int> yOffSetV = Vector.Create(yOffset);

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            for (int x = sectorFromX; x <= sectorToX; x++)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.FloorRenderable)
                {
                    continue;
                }

                int floorEndY = floorEnd[x];
                int wallEndY = wallEnd[x];

                int screenIndex = wallEndY * width + x;
                float xMapPosMultiplier = xMapPosMultiplierCache[x];

                RenderFloorOrCeilingColumn(ref screenPtr, ref floorTexturePtr, screenIndex, floorEndY, wallEndY, width,
                    x, yfloorV, xMapPosMultiplier, yOffSetV, xOffSetV, textureWidthV,
                    textureHeightMaskV, textureWidthMaskV, rotated, rSinV, rCosV, alignWallXV, alignWallYV, flipY, flipX, swapXy, doubleSize, sector, sector.Settings.HasFlag(MapSectorSettings.SlopeFloor) ? true : null);
            }
        }

        private void RenderFloorOrCeilingColumn(
            scoped ref uint screenPtr,
            scoped ref uint textureRef,
            int screenIndex,
            int floorToY,
            int floorFromY,
            int width,
            int x,
            Vector<float> cameraPosition,
            float xMapPosMultiplier,
            Vector<int> yOffSetV,
            Vector<int> xOffSetV,
            Vector<int> textureWidthV,
            Vector<int> textureHeightMaskV,
            Vector<int> textureWidthMaskV,
            bool rotated,
            Vector<float> rSinV,
            Vector<float> rCosV,
            Vector<float> alignXV,
            Vector<float> alignXY,
            bool flipY,
            bool flipX,
            bool swapXy,
            bool doubleSize,
            Sector sector,
            bool? slopeFloor
        )
        {
            int halfHeight = PixelHeight / 2;

            ref float incrCacheRef = ref memoryPool.GetBucketRef<float>(MemoryPoolBucket.CameraHeightToMapYPos);
            Vector<float> incramentVector = Vector.LoadUnsafe(ref Unsafe.Add(ref incrCacheRef, floorFromY));

            int rem = (floorToY - floorFromY) % Vector<int>.Count;
            floorToY -= rem;

            Vector<float> xMapPosMultiplierV = Vector.Create(xMapPosMultiplier);

            ref uint screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);
            ref readonly uint toScalePtr = ref Unsafe.Add(ref screenPtr, floorToY * width + x);

            Vector3 planePoint, planeNormal;
            Vector<float> nX, nY, nZ, pX, pY, pZ;

            if (slopeFloor is bool slopeV)
            {
                (planePoint, planeNormal) = slopeV ? MathFormulas.CalculatePlaneNormalFloor(sector)
                    : MathFormulas.CalculatePlaneNormalCeil(sector);

                // Convert scalar plane data into vectors
                nX = Vector.Create(planeNormal.X);
                nY = Vector.Create(planeNormal.Y);
                nZ = Vector.Create(planeNormal.Z);
                pX = Vector.Create(planePoint.X);
                pY = Vector.Create(planePoint.Y);
                pZ = Vector.Create(planePoint.Z);
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
            }

            while (!Unsafe.AreSame(in screenTex, in toScalePtr))
            {
                Vector<float> yMapPosR = cameraPosition * incramentVector;
                Vector<float> xMapPosR = yMapPosR * xMapPosMultiplierV;

                if (slopeFloor is not null)
                {
                    // starting point (p)
                    Vector<float> camera_position_z = pzV;

                    // direction vector
                    var dir_x = - xMapPosR;
                    var dir_y = - yMapPosR;
                    var dir_z = camera_position_z - Vector.Create<float>((bool)slopeFloor ? sector.Floor : sector.Ceil);

                    // (dir_x, dir_y, dir_z) = MathFormulas.NormalizeVector(dir_x, dir_y, dir_z);

                    // Vectorized intersection for the whole vector lane
                    MathFormulas.FindIntersectionVectorZero(nX, nY, nZ, pX, pY, pZ, camera_position_z,
                        dir_x, dir_y, dir_z,
                        out xMapPosR, out yMapPosR);
                }

                (Vector<float> xMapPos, Vector<float> yMapPos) = SharedHelpers.RotateVertexBack(xMapPosR, yMapPosR, pSinV, pCosV, pxV, pyV);

                if (rotated)
                {
                    xMapPos -= alignXV;
                    yMapPos -= alignXY;

                    Vector<float> xMapPosSR = Vector.FusedMultiplyAdd(xMapPos, rCosV, -yMapPos * rSinV);
                    Vector<float> yMapPosSR = Vector.FusedMultiplyAdd(xMapPos, rSinV, yMapPos * rCosV);

                    xMapPos = xMapPosSR;
                    yMapPos = yMapPosSR;
                }

                if (swapXy)
                {
                    (xMapPos, yMapPos) = (yMapPos, xMapPos);
                }

                Vector<int> _y1 = Vector.ConvertToInt32Native(yMapPos);
                Vector<int> _x1 = Vector.ConvertToInt32Native(xMapPos);

                if (doubleSize)
                {
                    _y1 >>= 1;
                    _x1 >>= 1;
                }

                _y1 = (_y1 + yOffSetV) & textureHeightMaskV;
                _x1 = (_x1 + xOffSetV) & textureWidthMaskV;         

                if (flipY)
                {
                    _y1 = textureHeightMaskV - _y1;
                }

                if (flipX)
                {
                    _x1 = textureWidthMaskV - _x1;
                }

                Vector<int> textureIndex = _y1 * textureWidthV + _x1;

                for (int i = 0; i < Vector<int>.Count; i++, screenTex = ref Unsafe.Add(ref screenTex, width))
                {
                    screenTex = Unsafe.Add(ref textureRef, textureIndex[i]);
                }

                floorFromY += Vector<float>.Count;
                incramentVector = Vector.LoadUnsafe(ref Unsafe.Add(ref incrCacheRef, floorFromY));
            }

            if (rem > 0)
            {
                Vector<float> yMapPosR = cameraPosition * incramentVector;
                Vector<float> xMapPosR = yMapPosR * xMapPosMultiplierV;

                if (slopeFloor is not null)
                {
                    Vector<float> camera_position_z = pzV;

                    // direction vector
                    var dir_x = - xMapPosR;
                    var dir_y = - yMapPosR;
                    var dir_z = camera_position_z - Vector.Create<float>((bool)slopeFloor ? sector.Floor : sector.Ceil);

                    (dir_x, dir_y, dir_z) = MathFormulas.NormalizeVector(dir_x, dir_y, dir_z);

                    // Vectorized intersection for the remainder lanes
                    MathFormulas.FindIntersectionVectorZero(nX, nY, nZ, pX, pY, pZ,
                        camera_position_z,
                        dir_x, dir_y, dir_z,
                        out xMapPosR, out yMapPosR);
                }

                (Vector<float> xMapPos, Vector<float> yMapPos) = SharedHelpers.RotateVertexBack(xMapPosR, yMapPosR, pSinV, pCosV, pxV, pyV);

                if (rotated)
                {
                    xMapPos -= alignXV;
                    yMapPos -= alignXY;

                    Vector<float> xMapPosSR = Vector.FusedMultiplyAdd(xMapPos, rCosV, -yMapPos * rSinV);
                    Vector<float> yMapPosSR = Vector.FusedMultiplyAdd(xMapPos, rSinV, yMapPos * rCosV);

                    xMapPos = xMapPosSR;
                    yMapPos = yMapPosSR;
                }

                if (swapXy)
                {
                    (xMapPos, yMapPos) = (yMapPos, xMapPos);
                }

                Vector<int> _y1 = Vector.ConvertToInt32Native(yMapPos);
                Vector<int> _x1 = Vector.ConvertToInt32Native(xMapPos);

                if (doubleSize)
                {
                    _y1 >>= 1;
                    _x1 >>= 1;
                }

                _y1 = (_y1 + yOffSetV) & textureHeightMaskV;
                _x1 = (_x1 + xOffSetV) & textureWidthMaskV;

                if (flipY)
                {
                    _y1 = textureHeightMaskV - _y1;
                }

                if (flipX)
                {
                    _x1 = textureWidthMaskV - _x1;
                }

                Vector<int> textureIndex = _y1 * textureWidthV + _x1;

                for (int i = 0; i < rem; i++, screenTex = ref Unsafe.Add(ref screenTex, width))
                {
                    screenTex = Unsafe.Add(ref textureRef, textureIndex[i]);
                }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (bool SwapXY, bool FlipX, bool FlipY, bool DoubleSize) GetFloorFlags(TextureInfo textureInfo)
        {
            TextureRenderingOptions options = textureInfo.RenderingOptions;

            bool swapXy = options.HasFlag(TextureRenderingOptions.SwapXY);
            bool flipX = options.HasFlag(TextureRenderingOptions.FlipX);
            bool flipY = options.HasFlag(TextureRenderingOptions.FlipY);
            bool doubleSize = textureInfo.XScale == 2 && textureInfo.YScale == 2;

            return (swapXy, flipX, flipY, doubleSize);
        }
    }
}
