using RenderingEngine.Models;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private Vector<int> pxVI = default;
        private Vector<int> pyVI = default;
        private Vector<int> pSinVI = default;
        private Vector<int> pCosVI = default;
        private Vector<float> ivIncrFI = default;

        private Vector<float> pxV = default;
        private Vector<float> pyV = default;
        private Vector<float> pSinV = default;
        private Vector<float> pCosV = default;
        private Vector<float> oneOvervFovV = default;
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
            oneOvervFovV = Vector.Create(oneOverHeight);
            ivIncrF = new(oneOverHeight * Vector<float>.Count);
            yawV = Vector.Create(yaw);
            oneOverHeightV = Vector.Create(oneOverHeight);

            pxVI = Vector.Create(float.ConvertToIntegerNative<int>(px * (1 << 16)));
            pyVI = Vector.Create(float.ConvertToIntegerNative<int>(py * (1 << 16)));
            pSinVI = Vector.Create(float.ConvertToIntegerNative<int>(pSin * (1 << 8)));
            pCosVI = Vector.Create(float.ConvertToIntegerNative<int>(pCos * (1 << 8)));
            ivIncrFI = Vector.Create(oneOverHeight * (Vector<float>.Count << 8));
        }

        [SkipLocalsInit]
        private void RenderCeilingVector(
            PortalPlayerSnapshot player,
            Sector sector,
            Span<BGRA> screen)
        {
            bool rotated = sector.RotationCeiling != 0f;

            if (!rotated || sector.RotationCeiling == EngineConstants.NinetyDegrees)
            {
                RenderCeilingVector2(player, sector, screen, rotated);
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

            int textureWidth = ceilingTexture.Width;
            int textureHeightMask = ceilingTexture.Height - 1;
            int textureWidthMask = ceilingTexture.Width - 1;
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
            ref BGRA screenPtr = ref MemoryMarshal.GetReference(screen);

            (int sectroFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            for (int x = sectroFromX; x < sectorToX; x++)
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
                    x, lightLevel, yCeilV, ref incramentVector, xMapPosMultiplier, yOffSetV, xOffSetV, textureWidthV,
                    textureHeightMaskV, textureWidthMaskV, rotated, rSinV, rCosV);
            }
        }

        [SkipLocalsInit]
        public void RenderCeilingVector2(
            PortalPlayerSnapshot player,
            Sector sector,
            Span<BGRA> screen,
            bool rotated)
        {
            byte lightLevel = sector.LightLevel;

            int height = PixelHeight;
            int width = PixelWidth;

            int yCeiling = float.ConvertToIntegerNative<int>(sector.Ceil - player.Z);

            float xPosIncr = 1f / (width * -EngineConstants.HeightToWidthRatio);

            int halfHeightInt = height / 2;
            int widthDiv2 = width / 2;

            TextureInfo textureInfo = sector.CeilTexture;
            ref Texture ceilingTexture = ref TextureCache.GetTexture(textureInfo.Name);

            ref BGRA floorTexturePtr = ref MemoryMarshal.GetArrayDataReference(rotated ? ceilingTexture.Rotated : ceilingTexture.Data);
            ref BGRA screenPtr = ref MemoryMarshal.GetReference(screen);

            Vector<float> yCeliningV = Vector.Create<float>(yCeiling << 16);

            int textureWidth = ceilingTexture.Width;
            int textureHeightMask = (rotated ? ceilingTexture.Width : ceilingTexture.Height) - 1;
            int textureWidthMask = (rotated ? ceilingTexture.Height : ceilingTexture.Width) - 1;

            Vector<int> textureHeightMaskV = Vector.Create(textureHeightMask);
            Vector<int> textureWidthMaskV = Vector.Create(textureWidthMask);
            Vector<int> textureWidthV = Vector.Create(textureWidth);

            int xOffset = -textureInfo.XOffset;
            int yOffset = textureInfo.YOffset;
            Vector<int> xOffSetV = Vector.Create(xOffset << 16);
            Vector<int> yOffSetV = Vector.Create(yOffset << 16);

            Unsafe.SkipInit(out Vector<float> incramentVector);

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            for (int x = sectorFromX; x < sectorToX; x++)
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

                incramentVector = Vector.CreateSequence((halfHeightInt - floorFromY) << 8, (float)-(1 << 8));
                incramentVector = Vector.FusedMultiplyAdd(incramentVector, oneOvervFovV, yawV);

                RenderFloorOrCeilingColumn(ref screenPtr, ref floorTexturePtr, screenIndex, floorToY, floorFromY, width,
                    x, lightLevel, yCeliningV, ref incramentVector, xMapPosMultiplier, yOffSetV, xOffSetV, textureWidthV,
                    textureHeightMaskV, textureWidthMaskV);
            }
        }


        private void RenderSkyboxVector(
            PortalPlayerSnapshot player,
            Span<BGRA> screen,
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
            ref BGRA screenPtr = ref MemoryMarshal.GetReference(screen);
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
            Span<BGRA> screen,
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
            ref BGRA screenPtr = ref MemoryMarshal.GetReference(screen);
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
        public void RenderFloorVector(
            PortalPlayerSnapshot player,
            Sector sector,
            Span<BGRA> screen)
        {
            bool rotated = sector.RotationFloor != 0f;
            TextureInfo textureInfo = sector.FloorTexture;

            if (!rotated || sector.RotationFloor == EngineConstants.NinetyDegrees)
            {
                RenderFloorVector2(player, sector, screen, rotated);
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

            ref BGRA floorTexturePtr = ref MemoryMarshal.GetArrayDataReference(floorTexture.Data);
            ref BGRA screenPtr = ref MemoryMarshal.GetReference(screen);

            Vector<float> yfloorV = Vector.Create(yfloor);

            int textureWidth = floorTexture.Width;
            int textureHeightMask = floorTexture.Height - 1;
            int textureWidthMask = floorTexture.Width - 1;

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

            Unsafe.SkipInit(out Vector<float> incramentVector);
            ref float incramentVectorPtr = ref Unsafe.As<Vector<float>, float>(ref incramentVector);

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            for (int x = sectorFromX; x < sectorToX; x++)
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
                incramentVector = Vector.FusedMultiplyAdd(incramentVector, oneOvervFovV, yawV);

                RenderFloorOrCeilingColumn(ref screenPtr, ref floorTexturePtr, screenIndex, floorToY, floorFromY, width,
                    x, lightLevel, yfloorV, ref incramentVector, xMapPosMultiplier, yOffSetV, xOffSetV, textureWidthV,
                    textureHeightMaskV, textureWidthMaskV, rotated, rSinV, rCosV);
            }
        }

        [SkipLocalsInit]
        public void RenderFloorVector2(
            PortalPlayerSnapshot player,
            Sector sector,
            Span<BGRA> screen,
            bool rotated)
        {
            byte lightLevel = sector.LightLevel;

            int height = PixelHeight;
            int width = PixelWidth;

            int yfloor = float.ConvertToIntegerNative<int>(sector.Floor - player.Z);

            float xPosIncr = 1f / (width * -EngineConstants.HeightToWidthRatio);

            int halfHeightInt = height / 2;
            int widthDiv2 = width / 2;

            TextureInfo textureInfo = sector.FloorTexture;
            ref Texture floorTexture = ref TextureCache.GetTexture(textureInfo.Name);

            ref BGRA floorTexturePtr = ref MemoryMarshal.GetArrayDataReference(rotated ? floorTexture.Rotated : floorTexture.Data);
            ref BGRA screenPtr = ref MemoryMarshal.GetReference(screen);

            Vector<float> yfloorV = Vector.Create<float>(yfloor << 16);

            int textureWidth = floorTexture.Width;
            int textureHeightMask = (rotated ? floorTexture.Width : floorTexture.Height) - 1;
            int textureWidthMask = (rotated ? floorTexture.Height : floorTexture.Width) - 1;

            Vector<int> textureHeightMaskV = Vector.Create(textureHeightMask);
            Vector<int> textureWidthMaskV = Vector.Create(textureWidthMask);
            Vector<int> textureWidthV = Vector.Create(textureWidth);

            int xOffset = -textureInfo.XOffset;
            int yOffset = textureInfo.YOffset;
            Vector<int> xOffSetV = Vector.Create(xOffset << 16);
            Vector<int> yOffSetV = Vector.Create(yOffset << 16);

            Unsafe.SkipInit(out Vector<float> incramentVector);

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            for (int x = sectorFromX; x < sectorToX; x++)
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

                incramentVector = Vector.CreateSequence((halfHeightInt - floorFromY) << 8, (float) -(1 << 8));
                incramentVector = Vector.FusedMultiplyAdd(incramentVector, oneOvervFovV, yawV);

                RenderFloorOrCeilingColumn(ref screenPtr, ref floorTexturePtr, screenIndex, floorToY, floorFromY, width,
                    x, lightLevel, yfloorV, ref incramentVector, xMapPosMultiplier, yOffSetV, xOffSetV, textureWidthV,
                    textureHeightMaskV, textureWidthMaskV);
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
            Vector<float> yCeilV, // (1 << 8)
            ref Vector<float> incramentVector, // 1 << 8
            int xMapPosMultiplier, // (1 << 8)
            Vector<int> yOffSetV, // (1 << 16)
            Vector<int> xOffSetV, // (1 << 16)
            Vector<int> textureWidthV,
            Vector<int> textureHeightMaskV,
            Vector<int> textureWidthMaskV
        )
        {
            int rem = (floorToY - floorFromY) % Vector<int>.Count;
            floorToY -= rem;

            Vector<int> xMapPosMultiplierV = Vector.Create(xMapPosMultiplier);

            ref BGRA screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);
            ref BGRA toScalePtr = ref Unsafe.Add(ref screenPtr, floorToY * width + x);

            while (!Unsafe.AreSame(ref screenTex, ref toScalePtr))
            {
                Vector<int> yMapPosR = Vector.ConvertToInt32Native(yCeilV / incramentVector);
                Vector<int> xMapPosR = yMapPosR * xMapPosMultiplierV;

                (Vector<int> xMapPos, Vector<int> yMapPos) = RotateVertexBack(
                    xMapPosR >> 10,
                    yMapPosR,
                    pSinVI, pCosVI, pxVI, pyVI);

                Vector<int> _y1 = ((yMapPos + yOffSetV) >> 16) & textureHeightMaskV;
                Vector<int> _x1 = ((xMapPos + xOffSetV) >> 16) & textureWidthMaskV;
                Vector<int> textureIndex = _y1 * textureWidthV + _x1;

                ref int textureIndexPtr = ref Unsafe.As<Vector<int>, int>(ref textureIndex);

                for (int i = 0; i < Vector<int>.Count; i++, screenTex = ref Unsafe.Add(ref screenTex, width))
                {
                    ref BGRA tex = ref Unsafe.Add(ref texturePtr, Unsafe.Add(ref textureIndexPtr, i));
                    ShadeByPrecalc(in tex, ref screenTex, lightLevel);
                }

                incramentVector -= ivIncrFI;
            }
            
            if (rem > 0)
            {
                Vector<int> yMapPosR = Vector.ConvertToInt32Native(yCeilV / incramentVector);
                Vector<int> xMapPosR = yMapPosR * xMapPosMultiplierV;

                (Vector<int> xMapPos, Vector<int> yMapPos) = RotateVertexBack(
                    xMapPosR >> 10,
                    yMapPosR,
                    pSinVI, pCosVI, pxVI, pyVI);

                Vector<int> _y1 = ((yMapPos + yOffSetV) >> 16) & textureHeightMaskV;
                Vector<int> _x1 = ((xMapPos + xOffSetV) >> 16) & textureWidthMaskV;
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
            ref Vector<float> incramentVector,
            float xMapPosMultiplier,
            Vector<int> yOffSetV,
            Vector<int> xOffSetV,
            Vector<int> textureWidthV,
            Vector<int> textureHeightMaskV,
            Vector<int> textureWidthMaskV,
            bool rotated,
            Vector<float> rSinV,
            Vector<float> rCosV
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

                (Vector<float> xMapPos, Vector<float> yMapPos) = RotateVertexBack(xMapPosR, yMapPosR, pSinV, pCosV, pxV, pyV);

                if (rotated)
                {
                    Vector<float> xMapPosSR, yMapPosSR;
                    xMapPosSR = xMapPos * rCosV - yMapPos * rSinV;
                    yMapPosSR = xMapPos * rSinV + yMapPos * rCosV;

                    xMapPos = xMapPosSR;
                    yMapPos = yMapPosSR;
                }

                Vector<int> _y1 = (Vector.ConvertToInt32Native(yMapPos) + yOffSetV) & textureHeightMaskV;
                Vector<int> _x1 = (Vector.ConvertToInt32Native(xMapPos) + xOffSetV) & textureWidthMaskV;
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

                (Vector<float> xMapPos, Vector<float> yMapPos) = RotateVertexBack(xMapPosR, yMapPosR, pSinV, pCosV, pxV, pyV);

                if (rotated)
                {
                    Vector<float> xMapPosSR, yMapPosSR;
                    xMapPosSR = xMapPos * rCosV - yMapPos * rSinV;
                    yMapPosSR = xMapPos * rSinV + yMapPos * rCosV;

                    xMapPos = xMapPosSR;
                    yMapPos = yMapPosSR;
                }

                Vector<int> _y1 = (Vector.ConvertToInt32Native(yMapPos) + yOffSetV) & textureHeightMaskV;
                Vector<int> _x1 = (Vector.ConvertToInt32Native(xMapPos) + xOffSetV) & textureWidthMaskV;
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
        private static (Vector<int> rx1, Vector<int> ry1) RotateVertexBack(
            Vector<int> x, Vector<int> y,
            Vector<int> psin, Vector<int> pcos,
            Vector<int> px, Vector<int> py)
        {
            Vector<int> rx1 = y * pcos + x * psin;
            Vector<int> ry1 = y * psin - x * pcos;

            return (rx1 + px, ry1 + py);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (Vector<float> rx1, Vector<float> ry1) RotateVertexBack(
            Vector<float> x, Vector<float> y,
            Vector<float> psin, Vector<float> pcos,
            Vector<float> px, Vector<float> py)
        {
            Vector<float> rx1 = y * pcos + x * psin;
            Vector<float> ry1 = y * psin - x * pcos;

            return (rx1 + px, ry1 + py);
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
