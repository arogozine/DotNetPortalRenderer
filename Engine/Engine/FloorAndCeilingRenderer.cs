using RenderingEngine.Models;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        #region Shared Precalculated Vectors

        private Vector<int> pxVI = default;
        private Vector<int> pyVI = default;
        private Vector<int> pSinVI = default;
        private Vector<int> pCosVI = default;

        private Vector<float> pxV = default;
        private Vector<float> pyV = default;
        private Vector<float> pSinV = default;
        private Vector<float> pCosV = default;
        private Vector<float> ivIncrF = default;
        private Vector<float> yawV = default;
        private Vector<float> oneOverHeightV = default;

        [SkipLocalsInit]
        public void InitializeSharedVectors(PortalPlayerSnapshot player)
        {
            float px = player.X;
            float py = player.Y;
            float pSin = player.Sin;
            float pCos = player.Cos;
            float yaw = player.Yaw;

            float oneOverHeight = 1f / PixelHeight;

            pxV = Vector.Create(px);
            pyV = Vector.Create(py);
            pSinV = Vector.Create(pSin);
            pCosV = Vector.Create(pCos);
            ivIncrF = new(oneOverHeight * Vector<float>.Count);
            yawV = Vector.Create(yaw);
            oneOverHeightV = Vector.Create(oneOverHeight);

            pxVI = Vector.Create(float.ConvertToIntegerNative<int>(px * (1 << 16)));
            pyVI = Vector.Create(float.ConvertToIntegerNative<int>(py * (1 << 16)));
            pSinVI = Vector.Create(float.ConvertToIntegerNative<int>(pSin * (1 << 8)));
            pCosVI = Vector.Create(float.ConvertToIntegerNative<int>(pCos * (1 << 8)));
        }

        #endregion

        [SkipLocalsInit]
        private void RenderCeilingVector(
            PortalPlayerSnapshot player,
            Sector sector)
        {
            bool rotated = sector.RotationCeiling != 0f;

            if (!rotated || sector.RotationCeiling == EngineConstants.NinetyDegrees)
            {
                RenderCeilingVector_FixedPoint(player, sector, rotated);
                return;
            }

            byte lightLevel = sector.LightLevel;

            int height = PixelHeight;
            int width = PixelWidth;
            int halfHeightInt = height / 2;
            int widthDiv2 = width / 2;

            float pz = player.Z;

            float xPosIncr = 1f / (width * -EngineConstants.HeightToWidthRatio);

            float yCeil = sector.Ceil - pz;

            Vector<float> yCeilV = Vector.Create(yCeil);

            Vector<float> incramentVector;
            TextureInfo ceilingTexture = sector.CeilTexture;
            (bool swapXy, bool flipX, bool flipY, bool doubleSize) = GetFloorFlags(ceilingTexture);

            int textureWidth = ceilingTexture.Width;
            int textureHeightMask = doubleSize ? (ceilingTexture.Height << 1) - 1 : ceilingTexture.Height - 1;
            int textureWidthMask = doubleSize ? (ceilingTexture.Width << 1) - 1 : ceilingTexture.Width - 1;
            Vector<int> textureHeightMaskV = Vector.Create(textureHeightMask);
            Vector<int> textureWidthMaskV = Vector.Create(textureWidthMask);
            Vector<int> textureWidthV = Vector.Create(textureWidth);

            int xOffset = -ceilingTexture.XOffset;
            int yOffset = ceilingTexture.YOffset;
            Vector<int> xOffSetV = Vector.Create(xOffset);
            Vector<int> yOffSetV = Vector.Create(yOffset);

            Unsafe.SkipInit(out Vector<float> rSinV);
            Unsafe.SkipInit(out Vector<float> rCosV);

            if (rotated)
            {
                (float rSin, float rCos) = MathF.SinCos(sector.RotationFloor + MathF.PI);
                rSinV = Vector.Create(rSin);
                rCosV = Vector.Create(rCos);
            }

            ref BGRA ceilingTexturePtr = ref MemoryMarshal.GetArrayDataReference(ceilingTexture.Data);
            ref BGRA screenPtr = ref GetScreenPtr<BGRA>();

            (int sectroFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            for (int x = sectroFromX; x <= sectorToX; x++)
            {
                RenderColumnStatus columnStatus = RenderWindowHelper.Status[x];

                if (!columnStatus.CeilingRenderable)
                {
                    continue;
                }

                int ceilingStart = RenderWindowHelper.CeilingStart[x];
                int wallStart = RenderWindowHelper.WallStart[x];
                int floorEnd = RenderWindowHelper.FloorEnd[x];
                int floorToY = Math.Clamp(wallStart, ceilingStart, floorEnd);

                int screenIndex = ceilingStart * width + x;

                float xMapPosMultiplier = (widthDiv2 - x) * xPosIncr;

                incramentVector = Vector.CreateSequence(halfHeightInt - ceilingStart, -1f);
                incramentVector = Vector.FusedMultiplyAdd(incramentVector, oneOverHeightV, yawV);

                RenderFloorOrCeilingColumn(ref screenPtr, ref ceilingTexturePtr, screenIndex, floorToY, ceilingStart, width,
                    x, lightLevel, yCeilV, incramentVector, xMapPosMultiplier, yOffSetV, xOffSetV, textureWidthV,
                    textureHeightMaskV, textureWidthMaskV, rotated, rSinV, rCosV, flipY, flipX, swapXy, doubleSize);
            }
        }

        [SkipLocalsInit]
        public void RenderCeilingVector_FixedPoint(
            PortalPlayerSnapshot player,
            Sector sector,
            bool rotated)
        {
            Span<RenderColumnStatus> status = RenderWindowHelper.Status;
            Span<int> ceilingStart = RenderWindowHelper.CeilingStart;
            Span<int> wallStart = RenderWindowHelper.WallStart;
            Span<int> floorEnd = RenderWindowHelper.FloorEnd;

            byte lightLevel = sector.LightLevel;

            int width = PixelWidth;

            int yCeiling = float.ConvertToIntegerNative<int>(sector.Ceil - player.Z);

            float xPosIncr = 1f / (width * -EngineConstants.HeightToWidthRatio);

            int widthDiv2 = width / 2;

            TextureInfo ceilingTexture = sector.CeilTexture;
            (bool swapXy, bool flipX, bool flipY, bool doubleSize) = GetFloorFlags(ceilingTexture);

            ref BGRA floorTexturePtr = ref MemoryMarshal.GetArrayDataReference(ceilingTexture.Data);
            ref BGRA screenPtr = ref GetScreenPtr<BGRA>();

            Vector<int> yCeliningV = Vector.Create(yCeiling);

            int textureWidth = ceilingTexture.Width;
            int textureHeight = ceilingTexture.Height;
            int textureHeightMask = doubleSize ? (ceilingTexture.Height << 1) - 1 : ceilingTexture.Height - 1;
            int textureWidthMask = doubleSize ? (ceilingTexture.Width << 1) - 1 : ceilingTexture.Width - 1;

            Vector<int> textureHeightMaskV = Vector.Create(textureHeightMask);
            Vector<int> textureWidthMaskV = Vector.Create(textureWidthMask);
            Vector<int> textureWidthV = Vector.Create(textureWidth);
            Vector<int> textureHeightV = Vector.Create(textureHeight);

            int xOffset = -ceilingTexture.XOffset;
            int yOffset = ceilingTexture.YOffset;
            Vector<int> xOffSetV = Vector.Create(xOffset << 16);
            Vector<int> yOffSetV = Vector.Create(yOffset << 16);

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            int length = (sectorToX - sectorFromX);

            if (Vector<float>.IsSupported && length > Vector<float>.Count)
            {
                const int canRenderCeilingMask = (int)(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderCeiling);

                Span<int> statusInt = MemoryMarshal.Cast<RenderColumnStatus, int>(status);
                Vector<int> widthV = Vector.Create(width);
                Vector<int> widthDiv2V = Vector.Create(widthDiv2);
                Vector<float> xPosIncrV = Vector.Create(xPosIncr);
                Vector<int> canRenderCeilingMaskV = Vector.Create(canRenderCeilingMask);

                Vector<int> sectorFromXV = Vector.CreateSequence(sectorFromX, 1);
                Vector<int> incr = Vector.Create(Vector<int>.Count);

                int rem = (sectorToX - sectorFromX) % Vector<float>.Count;
                sectorToX -= rem;

                for (int x = sectorFromX; x < sectorToX; sectorFromXV += incr)
                {
                    Vector<int> columnStatusV = Vector.LoadUnsafe(ref statusInt[x]) & canRenderCeilingMaskV;

                    if (columnStatusV == Vector<int>.Zero)
                    {
                        x += Vector<int>.Count;
                        continue;
                    }

                    Vector<int> ceilingStartV = Vector.LoadUnsafe(ref ceilingStart[x]);
                    Vector<int> wallStartV = Vector.LoadUnsafe(ref wallStart[x]);
                    Vector<int> floorEndV = Vector.LoadUnsafe(ref floorEnd[x]);

                    Vector<int> floorToV = Vector.Min(Vector.Max(wallStartV, ceilingStartV), floorEndV);
                    Vector<int> screenIndexV = ceilingStartV * widthV + sectorFromXV;
                    Vector<int> xMapPosMultiplierV = Vector.ConvertToInt32Native(Vector.ConvertToSingle((widthDiv2V - sectorFromXV) << 10) * xPosIncrV);

                    for (int i = 0; i < Vector<float>.Count; i++, x++)
                    {
                        if (columnStatusV[i] != canRenderCeilingMask)
                        {
                            continue;
                        }

                        int ceilingStartY = ceilingStartV[i];
                        int floorToY = floorToV[i];
                        int screenIndex = screenIndexV[i];
                        int xMapPosMultiplier = xMapPosMultiplierV[i];

                        RenderFloorOrCeilingColumn_FixedPoint(ref screenPtr, ref floorTexturePtr, screenIndex, floorToY, ceilingStartY, width,
                            x, lightLevel, yCeliningV, xMapPosMultiplier, yOffSetV, xOffSetV, textureWidthV, textureHeightV,
                            textureHeightMaskV, textureWidthMaskV, flipY, flipX, swapXy, rotated, doubleSize);
                    }
                }


                sectorFromX = sectorToX;
                sectorToX += rem;
            }

            for (int x = sectorFromX; x <= sectorToX; x++)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.CeilingRenderable)
                {
                    continue;
                }

                int ceilingStartY = ceilingStart[x];
                int wallStartY = wallStart[x];
                int floorEndY = floorEnd[x];
                int floorToY = Math.Clamp(wallStartY, ceilingStartY, floorEndY);

                int screenIndex = ceilingStartY * width + x;
                int xMapPosMultiplier = float.ConvertToIntegerNative<int>(((widthDiv2 - x) << 10) * xPosIncr);

                RenderFloorOrCeilingColumn_FixedPoint(ref screenPtr, ref floorTexturePtr, screenIndex, floorToY, ceilingStartY, width,
                    x, lightLevel, yCeliningV, xMapPosMultiplier, yOffSetV, xOffSetV, textureWidthV, textureHeightV,
                    textureHeightMaskV, textureWidthMaskV, flipY, flipX, swapXy, rotated, doubleSize);
            }
        }

        [SkipLocalsInit]
        public void RenderFloorVector(PortalPlayerSnapshot player, Sector sector)
        {
            bool rotated = sector.RotationFloor != 0f;
            TextureInfo floorTexture = sector.FloorTexture;

            if (!rotated || sector.RotationFloor == EngineConstants.NinetyDegrees)
            {
                RenderFloorVector_FixedPoint(player, sector, rotated);
                return;
            }

            ReadOnlySpan<RenderColumnStatus> status = RenderWindowHelper.Status;
            ReadOnlySpan<int> ceilingStart = RenderWindowHelper.CeilingStart;
            ReadOnlySpan<int> floorEnd = RenderWindowHelper.FloorEnd;
            ReadOnlySpan<int> wallEnd = RenderWindowHelper.WallEnd;

            byte lightLevel = sector.LightLevel;

            int height = PixelHeight;
            int width = PixelWidth;

            float yfloor = sector.Floor - player.Z;

            float xPosIncr = 1f / (width * -EngineConstants.HeightToWidthRatio);

            int halfHeightInt = height / 2;
            int widthDiv2 = width / 2;

            (bool swapXy, bool flipX, bool flipY, bool doubleSize) = GetFloorFlags(floorTexture);

            ref BGRA floorTexturePtr = ref MemoryMarshal.GetArrayDataReference(floorTexture.Data);
            ref BGRA screenPtr = ref GetScreenPtr<BGRA>();

            Vector<float> yfloorV = Vector.Create(yfloor);

            int textureWidth = floorTexture.Width;
            int textureHeightMask = doubleSize ? (floorTexture.Height << 1) - 1 : floorTexture.Height - 1;
            int textureWidthMask = doubleSize ? (floorTexture.Width << 1) - 1 : floorTexture.Width - 1;

            Vector<int> textureHeightMaskV = Vector.Create(textureHeightMask);
            Vector<int> textureWidthMaskV = Vector.Create(textureWidthMask);
            Vector<int> textureWidthV = Vector.Create(textureWidth);

            int xOffset = -floorTexture.XOffset;
            int yOffset = floorTexture.YOffset;
            Vector<int> xOffSetV = Vector.Create(xOffset);
            Vector<int> yOffSetV = Vector.Create(yOffset);

            Unsafe.SkipInit(out Vector<float> rSinV);
            Unsafe.SkipInit(out Vector<float> rCosV);

            if (rotated)
            {
                (float rSin, float rCos) = MathF.SinCos(sector.RotationFloor + MathF.PI);
                rSinV = Vector.Create(rSin);
                rCosV = Vector.Create(rCos);
            }

            Vector<float> incramentVector;

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            for (int x = sectorFromX; x <= sectorToX; x++)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.FloorRenderable)
                {
                    continue;
                }

                int ceilingStartY = ceilingStart[x];
                int floorEndY = floorEnd[x];
                int wallEndY = wallEnd[x];

                int floorFromY = Math.Clamp(wallEndY, ceilingStartY, floorEndY);

                int screenIndex = floorFromY * width + x;
                float xMapPosMultiplier = (widthDiv2 - x) * xPosIncr;

                incramentVector = Vector.CreateSequence(halfHeightInt - floorFromY, -1f);
                incramentVector = Vector.FusedMultiplyAdd(incramentVector, oneOverHeightV, yawV);

                RenderFloorOrCeilingColumn(ref screenPtr, ref floorTexturePtr, screenIndex, floorEndY, floorFromY, width,
                    x, lightLevel, yfloorV, incramentVector, xMapPosMultiplier, yOffSetV, xOffSetV, textureWidthV,
                    textureHeightMaskV, textureWidthMaskV, rotated, rSinV, rCosV, flipY, flipX, swapXy, doubleSize);
            }
        }

        [SkipLocalsInit]
        public void RenderFloorVector_FixedPoint(
            PortalPlayerSnapshot player,
            Sector sector,
            bool rotated)
        {
            Span<RenderColumnStatus> status = RenderWindowHelper.Status;
            Span<int> ceilingStart = RenderWindowHelper.CeilingStart;
            Span<int> floorEnd = RenderWindowHelper.FloorEnd;
            Span<int> wallEnd = RenderWindowHelper.WallEnd;

            byte lightLevel = sector.LightLevel;

            int width = PixelWidth;

            int yfloor = float.ConvertToIntegerNative<int>(sector.Floor - player.Z);

            float xPosIncr = 1f / (width * -EngineConstants.HeightToWidthRatio);

            int widthDiv2 = width / 2;

            TextureInfo floorTexture = sector.FloorTexture;
            (bool swapXy, bool flipX, bool flipY, bool doubleSize) = GetFloorFlags(floorTexture);

            ref BGRA floorTexturePtr = ref MemoryMarshal.GetArrayDataReference(floorTexture.Data);
            ref BGRA screenPtr = ref GetScreenPtr<BGRA>();

            Vector<int> yfloorV = Vector.Create(yfloor);

            int textureWidth = floorTexture.Width;
            int textureHeight = floorTexture.Height;
            int textureHeightMask = doubleSize ? (textureHeight << 1) - 1 : textureHeight - 1;
            int textureWidthMask = doubleSize ? (textureWidth << 1) - 1 : textureWidth - 1;

            Vector<int> textureHeightMaskV = Vector.Create(textureHeightMask);
            Vector<int> textureWidthMaskV = Vector.Create(textureWidthMask);
            Vector<int> textureWidthV = Vector.Create(textureWidth);
            Vector<int> textureHeightV = Vector.Create(textureHeight);

            int xOffset = -floorTexture.XOffset;
            int yOffset = floorTexture.YOffset;
            Vector<int> xOffSetV = Vector.Create(xOffset << 16);
            Vector<int> yOffSetV = Vector.Create(yOffset << 16);

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            int length = (sectorToX - sectorFromX);

            if (Vector<float>.IsSupported && length > Vector<float>.Count)
            {
                const int canRenderFloorMask = (int)(RenderColumnStatus.Calculated | RenderColumnStatus.CanRenderFloor);

                Span<int> statusInt = MemoryMarshal.Cast<RenderColumnStatus, int>(status);
                Vector<int> widthV = Vector.Create(width);
                Vector<int> widthDiv2V = Vector.Create(widthDiv2);
                Vector<float> xPosIncrV = Vector.Create(xPosIncr);
                Vector<int> canRenderFloorMaskV = Vector.Create(canRenderFloorMask);

                Vector<int> sectorFromXV = Vector.CreateSequence(sectorFromX, 1);
                Vector<int> incr = Vector.Create(Vector<int>.Count);

                int rem = (sectorToX - sectorFromX) % Vector<float>.Count;
                sectorToX -= rem;

                for (int x = sectorFromX; x < sectorToX; sectorFromXV += incr)
                {
                    Vector<int> columnStatusV = Vector.LoadUnsafe(ref statusInt[x]) & canRenderFloorMaskV;

                    if (columnStatusV == Vector<int>.Zero)
                    {
                        x += Vector<int>.Count;
                        continue;
                    }

                    Vector<int> ceilingStartV = Vector.LoadUnsafe(ref ceilingStart[x]);
                    Vector<int> floorEndV = Vector.LoadUnsafe(ref floorEnd[x]);
                    Vector<int> wallEndV = Vector.LoadUnsafe(ref wallEnd[x]);
                    Vector<int> floorFromV = Vector.Min(Vector.Max(wallEndV, ceilingStartV), floorEndV);
                    Vector<int> screenIndexV = floorFromV * widthV + sectorFromXV;
                    Vector<int> xMapPosMultiplierV = Vector.ConvertToInt32Native(Vector.ConvertToSingle((widthDiv2V - sectorFromXV) << 10) * xPosIncrV);

                    for (int i = 0; i < Vector<float>.Count; i++, x++)
                    {
                        if (columnStatusV[i] != canRenderFloorMask)
                        {
                            continue;
                        }

                        int floorFromY = floorFromV[i];
                        int floorEndY = floorEndV[i];
                        int screenIndex = screenIndexV[i];
                        int xMapPosMultiplier = xMapPosMultiplierV[i];

                        RenderFloorOrCeilingColumn_FixedPoint(ref screenPtr, ref floorTexturePtr, screenIndex, floorEndY, floorFromY, width,
                            x, lightLevel, yfloorV, xMapPosMultiplier, yOffSetV, xOffSetV, textureWidthV, textureHeightV,
                            textureHeightMaskV, textureWidthMaskV, flipY, flipX, swapXy, rotated, doubleSize);
                    }
                }

                sectorFromX = sectorToX;
                sectorToX += rem;
            }

            for (int x = sectorFromX; x <= sectorToX; x++)
            {
                RenderColumnStatus columnStatus = status[x];

                if (!columnStatus.FloorRenderable)
                {
                    continue;
                }

                int ceilingStartY = ceilingStart[x];
                int floorEndY = floorEnd[x];
                int wallEndY = wallEnd[x];

                int floorFromY = Math.Clamp(wallEndY, ceilingStartY, floorEndY);

                int screenIndex = floorFromY * width + x;
                int xMapPosMultiplier = float.ConvertToIntegerNative<int>(((widthDiv2 - x) << 10) * xPosIncr);

                RenderFloorOrCeilingColumn_FixedPoint(ref screenPtr, ref floorTexturePtr, screenIndex, floorEndY, floorFromY, width,
                    x, lightLevel, yfloorV, xMapPosMultiplier, yOffSetV, xOffSetV, textureWidthV, textureHeightV,
                    textureHeightMaskV, textureWidthMaskV, flipY, flipX, swapXy, rotated, doubleSize);
            }
        }

        private void RenderFloorOrCeilingColumn_FixedPoint(
            scoped ref BGRA screenPtr,
            scoped ref BGRA texturePtr,
            int screenIndex,
            int floorToY,
            int floorFromY,
            int width,
            int x,
            uint lightLevel,
            Vector<int> yCeilV, // 1 << 8
            int xMapPosMultiplier, // 1 << 10
            Vector<int> yOffSetV, // 1 << 16
            Vector<int> xOffSetV, // 1 << 16
            Vector<int> textureWidthV,
            Vector<int> textureHeightV,
            Vector<int> textureHeightMaskV,
            Vector<int> textureWidthMaskV,
            bool flipY,
            bool flipX,
            bool swapXy,
            bool rotated,
            bool doubleSize
        )
        {
            Span<int> incrVectorCache = SharedHelpers.AlignSpan(this.incrVectorCache);

            Vector<int> incramentVector = Vector.LoadUnsafe(ref incrVectorCache[floorFromY]);

            int rem = (floorToY - floorFromY) % Vector<int>.Count;
            floorToY -= rem;

            Vector<int> xMapPosMultiplierV = Vector.Create(xMapPosMultiplier);

            ref BGRA screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);
            ref readonly BGRA toScalePtr = ref Unsafe.Add(ref screenPtr, floorToY * width + x);

            while (!Unsafe.AreSame(in screenTex, in toScalePtr))
            {
                Vector<int> yMapPosR = yCeilV * incramentVector;
                Vector<int> xMapPosR = yMapPosR * xMapPosMultiplierV;

                (Vector<int> xMapPos, Vector<int> yMapPos) = SharedHelpers.RotateVertexBack(
                    xMapPosR >> 10,
                    yMapPosR,
                    pSinVI, pCosVI, pxVI, pyVI);

                Vector<int> _y1, _x1;

                // for non-floating point rotation, we only support 90 degrees for now
                if (rotated)
                {
                    (xMapPos, yMapPos) = (yMapPos, xMapPos);
                    yMapPos *= -1;
                }

                _y1 = ((xMapPos + xOffSetV) >> 16) & textureHeightMaskV;
                _x1 = ((yMapPos + yOffSetV) >> 16) & textureWidthMaskV;
                
                if (flipY)
                {
                    _y1 = textureHeightMaskV - _y1;
                }

                if (flipX)
                {
                    _x1 = textureWidthMaskV - _x1;
                }

                if (doubleSize)
                {
                    _y1 >>= 1;
                    _x1 >>= 1;
                }

                Vector<int> textureIndex;

                if (swapXy)
                {
                    (_y1, _x1) = (_x1, _y1);
                    textureIndex = _y1 * textureHeightV + _x1;
                }
                else
                {
                    textureIndex = _y1 * textureWidthV + _x1;
                }

                ref int textureIndexPtr = ref Unsafe.As<Vector<int>, int>(ref textureIndex);

                for (int i = 0; i < Vector<int>.Count; i++, screenTex = ref Unsafe.Add(ref screenTex, width))
                {
                    ref BGRA tex = ref Unsafe.Add(ref texturePtr, Unsafe.Add(ref textureIndexPtr, i));
                    ShadeByPrecalc(in tex, ref screenTex, lightLevel);
                }

                floorFromY += Vector<float>.Count;
                incramentVector = Vector.LoadUnsafe(ref incrVectorCache[floorFromY]);
            }
            
            // tail that doesn't fit ovenly into a vector
            if (rem > 0)
            {
                Vector<int> yMapPosR = yCeilV * incramentVector;
                Vector<int> xMapPosR = yMapPosR * xMapPosMultiplierV;

                (Vector<int> xMapPos, Vector<int> yMapPos) = SharedHelpers.RotateVertexBack(
                    xMapPosR >> 10,
                    yMapPosR,
                    pSinVI, pCosVI, pxVI, pyVI);

                if (rotated)
                {
                    (xMapPos, yMapPos) = (yMapPos, xMapPos);
                    yMapPos *= -1;
                }

                Vector<int> _y1, _x1;

                _y1 = ((xMapPos + xOffSetV) >> 16) & textureHeightMaskV;
                _x1 = ((yMapPos + yOffSetV) >> 16) & textureWidthMaskV;

                if (flipY)
                {
                    _y1 = textureHeightMaskV - _y1;
                }

                if (flipX)
                {
                    _x1 = textureWidthMaskV - _x1;
                }

                if (doubleSize)
                {
                    _y1 >>= 1;
                    _x1 >>= 1;
                }

                Vector<int> textureIndex;

                if (swapXy)
                {
                    (_y1, _x1) = (_x1, _y1);
                    textureIndex = _y1 * textureHeightV + _x1;
                }
                else
                {
                    textureIndex = _y1 * textureWidthV + _x1;
                }

                ref int textureIndexPtr = ref Unsafe.As<Vector<int>, int>(ref textureIndex);

                for (int i = 0; i < rem; i++, screenTex = ref Unsafe.Add(ref screenTex, width))
                {
                    ref BGRA tex = ref Unsafe.Add(ref texturePtr, Unsafe.Add(ref textureIndexPtr, i));
                    ShadeByPrecalc(in tex, ref screenTex, lightLevel);
                }
            }

        }


        private void RenderFloorOrCeilingColumn(
            scoped ref BGRA screenPtr,
            scoped ref BGRA texturePtr,
            int screenIndex,
            int floorToY,
            int floorFromY,
            int width,
            int x,
            uint lightLevel,
            Vector<float> yCeilV,
            Vector<float> incramentVector,
            float xMapPosMultiplier,
            Vector<int> yOffSetV,
            Vector<int> xOffSetV,
            Vector<int> textureWidthV,
            Vector<int> textureHeightMaskV,
            Vector<int> textureWidthMaskV,
            bool rotated,
            Vector<float> rSinV,
            Vector<float> rCosV,
            bool flipY,
            bool flipX,
            bool swapXy,
            bool doubleSize
        )
        {
            int rem = (floorToY - floorFromY) % Vector<int>.Count;
            floorToY -= rem;

            Vector<float> xMapPosMultiplierV = Vector.Create(xMapPosMultiplier);

            ref BGRA screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);
            ref readonly BGRA toScalePtr = ref Unsafe.Add(ref screenPtr, floorToY * width + x);

            while (!Unsafe.AreSame(in screenTex, in toScalePtr))
            {
                Vector<float> yMapPosR = yCeilV / incramentVector;
                Vector<float> xMapPosR = yMapPosR * xMapPosMultiplierV;

                (Vector<float> xMapPos, Vector<float> yMapPos) = SharedHelpers.RotateVertexBack(xMapPosR, yMapPosR, pSinV, pCosV, pxV, pyV);

                if (rotated)
                {
                    Vector<float> xMapPosSR = xMapPos * rCosV - yMapPos * rSinV;
                    Vector<float> yMapPosSR = xMapPos * rSinV + yMapPos * rCosV;

                    xMapPos = xMapPosSR;
                    yMapPos = yMapPosSR;
                }

                Vector<int> _y1 = Vector.ConvertToInt32Native(yMapPos);
                Vector<int> _x1 = Vector.ConvertToInt32Native(xMapPos);

                _y1 = (_y1 + xOffSetV) & textureHeightMaskV;
                _x1 = (_x1 + yOffSetV) & textureWidthMaskV;         

                if (flipY)
                {
                    _y1 = textureHeightMaskV - _y1;
                }

                if (flipX)
                {
                    _x1 = textureWidthMaskV - _x1;
                }

                if (doubleSize)
                {
                    _y1 >>= 1;
                    _x1 >>= 1;
                }

                if (swapXy)
                {
                    (_y1, _x1) = (_x1, _y1);
                }

                Vector<int> textureIndex = _y1 * textureWidthV + _x1;

                ref int textureIndexPtr = ref Unsafe.As<Vector<int>, int>(ref textureIndex);

                for (int i = 0; i < Vector<int>.Count; i++, screenTex = ref Unsafe.Add(ref screenTex, width))
                {
                    ref BGRA tex = ref Unsafe.Add(ref texturePtr, Unsafe.Add(ref textureIndexPtr, i));
                    ShadeByPrecalc(in tex, ref screenTex, lightLevel);
                }

                incramentVector -= ivIncrF;
            }

            if (rem > 0)
            {
                Vector<float> yMapPosR = yCeilV / incramentVector;
                Vector<float> xMapPosR = yMapPosR * xMapPosMultiplierV;

                (Vector<float> xMapPos, Vector<float> yMapPos) = SharedHelpers.RotateVertexBack(xMapPosR, yMapPosR, pSinV, pCosV, pxV, pyV);

                if (rotated)
                {
                    Vector<float> xMapPosSR = xMapPos * rCosV - yMapPos * rSinV;
                    Vector<float> yMapPosSR = xMapPos * rSinV + yMapPos * rCosV;

                    xMapPos = xMapPosSR;
                    yMapPos = yMapPosSR;
                }

                Vector<int> _y1 = Vector.ConvertToInt32Native(yMapPos);
                Vector<int> _x1 = Vector.ConvertToInt32Native(xMapPos);

                _y1 = (_y1 + xOffSetV) & textureHeightMaskV;
                _x1 = (_x1 + yOffSetV) & textureWidthMaskV;

                if (flipY)
                {
                    _y1 = textureHeightMaskV - _y1;
                }

                if (flipX)
                {
                    _x1 = textureWidthMaskV - _x1;
                }

                if (doubleSize)
                {
                    _y1 >>= 1;
                    _x1 >>= 1;
                }

                if (swapXy)
                {
                    (_y1, _x1) = (_x1, _y1);
                }

                Vector<int> textureIndex = _y1 * textureWidthV + _x1;

                ref int textureIndexPtr = ref Unsafe.As<Vector<int>, int>(ref textureIndex);

                for (int i = 0; i < rem; i++, screenTex = ref Unsafe.Add(ref screenTex, width))
                {
                    ref BGRA tex = ref Unsafe.Add(ref texturePtr, Unsafe.Add(ref textureIndexPtr, i));
                    ShadeByPrecalc(in tex, ref screenTex, lightLevel);
                }
            }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ShadeByPrecalc(in BGRA inColor, ref BGRA outColor, uint scale)
        {
            const uint Alpha = (uint)byte.MaxValue << 24;

            uint b = inColor.B * scale >> 8;
            uint g = inColor.G * scale >> 8 << 8;
            uint r = inColor.R * scale >> 8 << 16;

            uint bgra = b | g | r | Alpha;

            Unsafe.As<BGRA, uint>(ref outColor) = bgra;
        }
    }
}
