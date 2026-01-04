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

            Unsafe.SkipInit(out Vector<float> incramentVector);
            ref float incramentVectorPtr = ref Unsafe.As<Vector<float>, float>(ref incramentVector);

            TextureInfo textureInfo = sector.CeilTexture;
            ref Texture ceilingTexture = ref TextureCache.GetTexture(textureInfo.Name);
            (bool swapXy, bool flipX, bool flipY, bool doubleSize) = GetFloorFlags(textureInfo);

            int textureWidth = ceilingTexture.Width;
            int textureHeightMask = doubleSize ? (ceilingTexture.Height << 1) - 1 : ceilingTexture.Height - 1;
            int textureWidthMask = doubleSize ? (ceilingTexture.Width << 1) - 1 : ceilingTexture.Width - 1;
            Vector<int> textureHeightMaskV = Vector.Create(textureHeightMask);
            Vector<int> textureWidthMaskV = Vector.Create(textureWidthMask);
            Vector<int> textureWidthV = Vector.Create(textureWidth);

            int xOffset = -textureInfo.XOffset;
            int yOffset = textureInfo.YOffset;
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
                ref RenderWindow renderWindow = ref RenderWindowHelper.RenderWindow[x];

                if (!renderWindow.CanRenderCeiling)
                {
                    continue;
                }

                int floorFromY = renderWindow.CeilingStart;
                int floorToY = Math.Clamp(renderWindow.WallStart, renderWindow.CeilingStart, renderWindow.FloorEnd);

                int screenIndex = floorFromY * width + x;

                float xMapPosMultiplier = (widthDiv2 - x) * xPosIncr;

                incramentVector = Vector.CreateSequence(halfHeightInt - floorFromY, -1f);
                incramentVector = Vector.FusedMultiplyAdd(incramentVector, oneOverHeightV, yawV);

                RenderFloorOrCeilingColumn(ref screenPtr, ref ceilingTexturePtr, screenIndex, floorToY, floorFromY, width,
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
            byte lightLevel = sector.LightLevel;

            int width = PixelWidth;

            int yCeiling = float.ConvertToIntegerNative<int>(sector.Ceil - player.Z);

            float xPosIncr = 1f / (width * -EngineConstants.HeightToWidthRatio);

            int widthDiv2 = width / 2;

            TextureInfo textureInfo = sector.CeilTexture;
            ref Texture ceilingTexture = ref TextureCache.GetTexture(textureInfo.Name);
            (bool swapXy, bool flipX, bool flipY, bool doubleSize) = GetFloorFlags(textureInfo);

            ref BGRA floorTexturePtr = ref MemoryMarshal.GetArrayDataReference(ceilingTexture.Data);
            ref BGRA screenPtr = ref GetScreenPtr<BGRA>();

            Vector<int> yCeliningV = Vector.Create<int>(yCeiling);

            int textureWidth = ceilingTexture.Width;
            int textureHeightMask = doubleSize ? (ceilingTexture.Height << 1) - 1 : ceilingTexture.Height - 1;
            int textureWidthMask = doubleSize ? (ceilingTexture.Width << 1) - 1 : ceilingTexture.Width - 1;

            Vector<int> textureHeightMaskV = Vector.Create(textureHeightMask);
            Vector<int> textureWidthMaskV = Vector.Create(textureWidthMask);
            Vector<int> textureWidthV = Vector.Create(textureWidth);

            int xOffset = -textureInfo.XOffset;
            int yOffset = textureInfo.YOffset;
            Vector<int> xOffSetV = Vector.Create(xOffset << 16);
            Vector<int> yOffSetV = Vector.Create(yOffset << 16);

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            for (int x = sectorFromX; x <= sectorToX; x++)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.RenderWindow[x];

                if (!renderWindow.CanRenderCeiling)
                {
                    continue;
                }

                int floorFromY = renderWindow.CeilingStart;
                int floorToY = Math.Clamp(renderWindow.WallStart, renderWindow.CeilingStart, renderWindow.FloorEnd);

                int screenIndex = floorFromY * width + x;
                int xMapPosMultiplier = float.ConvertToIntegerNative<int>(((widthDiv2 - x) << 10) * xPosIncr);

                RenderFloorOrCeilingColumn_FixedPoint(ref screenPtr, ref floorTexturePtr, screenIndex, floorToY, floorFromY, width,
                    x, lightLevel, yCeliningV, xMapPosMultiplier, yOffSetV, xOffSetV, textureWidthV,
                    textureHeightMaskV, textureWidthMaskV, flipY, flipX, swapXy, rotated, doubleSize);
            }
        }

        private void RenderSkyboxVector(
            PortalPlayerSnapshot player,
            Sector sector
            )
        {
            const float twoPi = 2 * MathF.PI;
            const float oneOverTwoPi = 1f / (2 * MathF.PI);

            int width = PixelWidth;
            int height = PixelHeight;
            float viewAngle = player.Angle;

            TextureInfo textureInfo = sector.CeilTexture;
            ref Texture texture = ref TextureCache.GetTexture(textureInfo.Name);
            ref BGRA ceilingTexturePtr = ref MemoryMarshal.GetArrayDataReference(texture.Data);
            ref BGRA screenPtr = ref GetScreenPtr<BGRA>();
            ref float angleCachePtr = ref MemoryMarshal.GetArrayDataReference(angleCache);

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            int textureWidth = texture.Width;
            int textureHeight = texture.Height;

            float textureWidth4 = textureWidth * 4f * oneOverTwoPi;

            int yTextureIncr = float.ConvertToInteger<int>((1f / height) * (textureHeight << 16));

            Vector<int> textureWidthV = Vector.Create(texture.Width);
            Vector<int> ivIncrF = new(yTextureIncr * Vector<float>.Count);

            Vector<int> incramentVector = Vector.CreateSequence(0, yTextureIncr);

            for (int x = sectorFromX; x <= sectorToX; x++)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.RenderWindow[x];

                if (!renderWindow.CanRenderCeiling)
                {
                    continue;
                }

                // calculate angle between 0 to 2 PI
                float angleX = Unsafe.Add(ref angleCachePtr, x) - viewAngle;

                if (angleX > twoPi)
                {
                    angleX -= twoPi;
                }
                else if (angleX < 0f)
                {
                    angleX = twoPi + angleX;
                }

                int texX = float.ConvertToIntegerNative<int>(textureWidth4 * angleX) % textureWidth;

                int ceilingStart = renderWindow.CeilingStart;
                int wallStartClamped = Math.Clamp(renderWindow.WallStart, renderWindow.CeilingStart, renderWindow.FloorEnd);

                int rem = (wallStartClamped - ceilingStart) % Vector<int>.Count;
                wallStartClamped -= rem;

                ref BGRA screenColumnPtr = ref Unsafe.Add(ref screenPtr, x + width * ceilingStart);
                ref BGRA screenEndColumnPtr = ref Unsafe.Add(ref screenPtr, x + width * wallStartClamped);
                ref BGRA textureColumnPtr = ref Unsafe.Add(ref ceilingTexturePtr, texX);

                int vScreen = ceilingStart * yTextureIncr;
                Vector<int> vScreenV = Vector.Create(vScreen) + incramentVector;

                for (; !Unsafe.AreSame(ref screenColumnPtr, ref screenEndColumnPtr); vScreenV += ivIncrF)
                {
                    Vector<int> texY = (vScreenV >> 16) * textureWidthV;

                    ref int texYPtr = ref Unsafe.As<Vector<int>, int>(ref texY);

                    for (int j = 0; j < Vector<int>.Count; j++)
                    {
                        int index = Unsafe.Add(ref texYPtr, j);

                        screenColumnPtr = Unsafe.Add(ref textureColumnPtr, index);
                        screenColumnPtr = ref Unsafe.Add(ref screenColumnPtr, width);
                    }
                }

                Vector<int> vScreenVInt = textureWidthV * (vScreenV >> 16);

                for (int y = 0; y < rem; y++)
                {
                    int index = vScreenVInt[y];
                    screenColumnPtr = Unsafe.Add(ref textureColumnPtr, index);
                    screenColumnPtr = ref Unsafe.Add(ref screenColumnPtr, width);
                }
            }
        }

        private void RenderSkyboxFloorVector(
            PortalPlayerSnapshot player,
            Sector sector
            )
        {
            const float twoPi = 2 * MathF.PI;
            const float oneOverTwoPi = 1f / (2 * MathF.PI);

            int width = PixelWidth;
            int height = PixelHeight;
            float viewAngle = player.Angle;

            TextureInfo textureInfo = sector.FloorTexture;
            ref Texture texture = ref TextureCache.GetTexture(textureInfo.Name);
            ref BGRA ceilingTexturePtr = ref MemoryMarshal.GetArrayDataReference(texture.Data);
            ref BGRA screenPtr = ref GetScreenPtr<BGRA>();
            ref float angleCachePtr = ref MemoryMarshal.GetArrayDataReference(angleCache);

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            int textureWidth = texture.Width;
            int textureHeight = texture.Height;

            float textureWidth4 = textureWidth * 4f * oneOverTwoPi;
            float yTextureIncr = (1f / height) * textureHeight;

            Vector<int> textureWidthV = Vector.Create(texture.Width);

            Vector<float> ivIncrF = new(yTextureIncr * Vector<float>.Count);

            Vector<float> incramentVector = Vector.CreateSequence(0f, yTextureIncr);

            for (int x = sectorFromX; x <= sectorToX; x++)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.RenderWindow[x];

                if (!renderWindow.CanRenderFloor)
                {
                    continue;
                }

                // calculate angle between 0 to 2 PI
                float angleX = Unsafe.Add(ref angleCachePtr, x) - viewAngle;

                if (angleX > twoPi)
                {
                    angleX -= twoPi;
                }
                else if (angleX < 0f)
                {
                    angleX = twoPi + angleX;
                }

                int texX = float.ConvertToIntegerNative<int>(textureWidth4 * angleX) % textureWidth;

                int wallStartClamped = Math.Clamp(renderWindow.WallStart, renderWindow.CeilingStart, renderWindow.FloorEnd);
                int floorEnd = renderWindow.FloorEnd;

                int rem = (floorEnd - wallStartClamped) % Vector<int>.Count;
                floorEnd -= rem;

                ref BGRA screenColumnPtr = ref Unsafe.Add(ref screenPtr, x + width * wallStartClamped);
                ref BGRA screenEndColumnPtr = ref Unsafe.Add(ref screenPtr, x + width * floorEnd);
                ref BGRA textureColumnPtr = ref Unsafe.Add(ref ceilingTexturePtr, texX);

                float vScreen = wallStartClamped * yTextureIncr;
                Vector<float> vScreenV = Vector.Create(vScreen) + incramentVector;

                for (; !Unsafe.AreSame(ref screenColumnPtr, ref screenEndColumnPtr); vScreenV += ivIncrF)
                {
                    Vector<int> texY = Vector.ConvertToInt32Native(vScreenV) * textureWidthV;

                    ref int texYPtr = ref Unsafe.As<Vector<int>, int>(ref texY);

                    for (int j = 0; j < Vector<int>.Count; j++)
                    {
                        int index = Unsafe.Add(ref texYPtr, j);

                        screenColumnPtr = Unsafe.Add(ref textureColumnPtr, index);
                        screenColumnPtr = ref Unsafe.Add(ref screenColumnPtr, width);
                    }
                }

                Vector<int> vScreenVInt = Vector.ConvertToInt32Native(vScreenV);

                for (int y = 0; y < rem; y++)
                {
                    int index = vScreenVInt[y] * textureWidth;

                    screenColumnPtr = Unsafe.Add(ref textureColumnPtr, index);
                    screenColumnPtr = ref Unsafe.Add(ref screenColumnPtr, width);
                }
            }

        }

        [SkipLocalsInit]
        public void RenderFloorVector(PortalPlayerSnapshot player, Sector sector)
        {
            bool rotated = sector.RotationFloor != 0f;
            TextureInfo textureInfo = sector.FloorTexture;

            if (!rotated || sector.RotationFloor == EngineConstants.NinetyDegrees)
            {
                RenderFloorVector_FixedPoint(player, sector, rotated);
                return;
            }

            byte lightLevel = sector.LightLevel;

            int height = PixelHeight;
            int width = PixelWidth;

            float yfloor = sector.Floor - player.Z;

            float xPosIncr = 1f / (width * -EngineConstants.HeightToWidthRatio);

            int halfHeightInt = height / 2;
            int widthDiv2 = width / 2;

            ref Texture floorTexture = ref TextureCache.GetTexture(textureInfo.Name);
            (bool swapXy, bool flipX, bool flipY, bool doubleSize) = GetFloorFlags(textureInfo);

            ref BGRA floorTexturePtr = ref MemoryMarshal.GetArrayDataReference(floorTexture.Data);
            ref BGRA screenPtr = ref GetScreenPtr<BGRA>();

            Vector<float> yfloorV = Vector.Create(yfloor);

            int textureWidth = floorTexture.Width;
            int textureHeightMask = doubleSize ? (floorTexture.Height << 1) - 1 : floorTexture.Height - 1;
            int textureWidthMask = doubleSize ? (floorTexture.Width << 1) - 1 : floorTexture.Width - 1;

            Vector<int> textureHeightMaskV = Vector.Create(textureHeightMask);
            Vector<int> textureWidthMaskV = Vector.Create(textureWidthMask);
            Vector<int> textureWidthV = Vector.Create(textureWidth);

            int xOffset = -textureInfo.XOffset;
            int yOffset = textureInfo.YOffset;
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
                ref RenderWindow renderWindow = ref RenderWindowHelper.RenderWindow[x];

                if (!renderWindow.CanRenderFloor)
                {
                    continue;
                }

                int floorFromY = Math.Clamp(renderWindow.WallEnd, renderWindow.CeilingStart, renderWindow.FloorEnd);
                int floorToY = renderWindow.FloorEnd;

                int screenIndex = floorFromY * width + x;
                float xMapPosMultiplier = (widthDiv2 - x) * xPosIncr;

                incramentVector = Vector.CreateSequence(halfHeightInt - floorFromY, -1f);
                incramentVector = Vector.FusedMultiplyAdd(incramentVector, oneOverHeightV, yawV);

                RenderFloorOrCeilingColumn(ref screenPtr, ref floorTexturePtr, screenIndex, floorToY, floorFromY, width,
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
            byte lightLevel = sector.LightLevel;

            int width = PixelWidth;

            int yfloor = float.ConvertToIntegerNative<int>(sector.Floor - player.Z);

            float xPosIncr = 1f / (width * -EngineConstants.HeightToWidthRatio);

            int widthDiv2 = width / 2;

            TextureInfo textureInfo = sector.FloorTexture;
            ref Texture floorTexture = ref TextureCache.GetTexture(textureInfo.Name);
            (bool swapXy, bool flipX, bool flipY, bool doubleSize) = GetFloorFlags(textureInfo);

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

            int xOffset = -textureInfo.XOffset;
            int yOffset = textureInfo.YOffset;
            Vector<int> xOffSetV = Vector.Create(xOffset << 16);
            Vector<int> yOffSetV = Vector.Create(yOffset << 16);

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            for (int x = sectorFromX; x <= sectorToX; x++)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.RenderWindow[x];

                if (!renderWindow.CanRenderFloor)
                {
                    continue;
                }

                int floorFromY = Math.Clamp(renderWindow.WallEnd, renderWindow.CeilingStart, renderWindow.FloorEnd);
                int floorToY = renderWindow.FloorEnd;

                int screenIndex = floorFromY * width + x;
                int xMapPosMultiplier = float.ConvertToIntegerNative<int>(((widthDiv2 - x) << 10) * xPosIncr);

                RenderFloorOrCeilingColumn_FixedPoint(ref screenPtr, ref floorTexturePtr, screenIndex, floorToY, floorFromY, width,
                    x, lightLevel, yfloorV, xMapPosMultiplier, yOffSetV, xOffSetV, textureWidthV,
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
            Vector<int> textureHeightMaskV,
            Vector<int> textureWidthMaskV,
            bool flipY,
            bool flipX,
            bool swapXy,
            bool rotated,
            bool doubleSize
        )
        {
            Span<int> incrVectorCache = MathFormulas.AlignSpan(this.incrVectorCache);

            Vector<int> incramentVector = Vector.LoadUnsafe(ref incrVectorCache[floorFromY]);

            int rem = (floorToY - floorFromY) % Vector<int>.Count;
            floorToY -= rem;

            Vector<int> xMapPosMultiplierV = Vector.Create(xMapPosMultiplier);

            ref BGRA screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);
            ref BGRA toScalePtr = ref Unsafe.Add(ref screenPtr, floorToY * width + x);

            while (!Unsafe.AreSame(ref screenTex, ref toScalePtr))
            {
                Vector<int> yMapPosR = yCeilV * incramentVector;
                Vector<int> xMapPosR = yMapPosR * xMapPosMultiplierV;

                (Vector<int> xMapPos, Vector<int> yMapPos) = MathFormulas.RotateVertexBack(
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

                floorFromY += Vector<float>.Count;
                incramentVector = Vector.LoadUnsafe(ref incrVectorCache[floorFromY]);
            }
            
            // tail that doesn't fit ovenly into a vector
            if (rem > 0)
            {
                Vector<int> yMapPosR = yCeilV * incramentVector;
                Vector<int> xMapPosR = yMapPosR * xMapPosMultiplierV;

                (Vector<int> xMapPos, Vector<int> yMapPos) = MathFormulas.RotateVertexBack(
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
            ref BGRA toScalePtr = ref Unsafe.Add(ref screenPtr, floorToY * width + x);

            while (!Unsafe.AreSame(ref screenTex, ref toScalePtr))
            {
                Vector<float> yMapPosR = yCeilV / incramentVector;
                Vector<float> xMapPosR = yMapPosR * xMapPosMultiplierV;

                (Vector<float> xMapPos, Vector<float> yMapPos) = MathFormulas.RotateVertexBack(xMapPosR, yMapPosR, pSinV, pCosV, pxV, pyV);

                if (rotated)
                {
                    Vector<float> xMapPosSR, yMapPosSR;
                    xMapPosSR = xMapPos * rCosV - yMapPos * rSinV;
                    yMapPosSR = xMapPos * rSinV + yMapPos * rCosV;

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

                (Vector<float> xMapPos, Vector<float> yMapPos) = MathFormulas.RotateVertexBack(xMapPosR, yMapPosR, pSinV, pCosV, pxV, pyV);

                if (rotated)
                {
                    Vector<float> xMapPosSR, yMapPosSR;
                    xMapPosSR = xMapPos * rCosV - yMapPos * rSinV;
                    yMapPosSR = xMapPos * rSinV + yMapPos * rCosV;

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
            unchecked
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
}
