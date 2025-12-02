using RenderingEngine.Models;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        [SkipLocalsInit]
        private void RenderCeiling(
            PortalPlayerSnapshot player,
            Sector sector,
            Span<BGRA> screen)
        {
            byte lightLevel = sector.LightLevel;

            int height = PixelHeight;
            int width = PixelWidth;
            int halfHeightInt = height / 2;
            float oneOverHeight = 1f / height;
            int widthDiv2 = width / 2;

            float px = player.X;
            float py = player.Y;
            float pz = player.Z;
            float pSin = player.Sin;
            float pCos = player.Cos;

            float xPosIncr = 1f / (width * -EngineConstants.HeightToWidthRatio);

            float yaw = player.Yaw;
            float yCeil = sector.Ceil - pz;

            TextureInfo textureInfo = sector.CeilTexture;
            ref Texture ceilingTexture = ref TextureCache.GetTexture(textureInfo.Name);

            int textureWidth = ceilingTexture.Width;
            int textureHeightMask = ceilingTexture.Height - 1;
            int textureWidthMask = ceilingTexture.Width - 1;

            ref BGRA ceilingTexturePtr = ref MemoryMarshal.GetArrayDataReference(ceilingTexture.Data);
            ref BGRA screenPtr = ref MemoryMarshal.GetReference(screen);

            (int sectroFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            for (int x = sectroFromX; x <= sectorToX; x++)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.GetCeilingDimensions(x);

                if (Unsafe.IsNullRef(ref renderWindow) || renderWindow.CeilingStart >= renderWindow.WallStart)
                {
                    continue;
                }

                int floorFromY = renderWindow.CeilingStart;
                int floorToY = renderWindow.WallStart;

                int screenIndex = floorFromY * width + x;

                float xMapPosMultiplier = (widthDiv2 - x) * xPosIncr;
                float ii = (halfHeightInt - floorFromY) * oneOverHeight + yaw;

                for (int j = floorFromY; j < floorToY; j++, screenIndex += width, ii -= oneOverHeight)
                {
                    float yMapPosR = yCeil / (ii + yaw);
                    float xMapPosR = yMapPosR * xMapPosMultiplier;

                    (float xMapPos, float yMapPos) = RotateVertexBack(xMapPosR, yMapPosR, pSin, pCos, px, py);

                    int _y1 = ((int)(yMapPos) + textureInfo.YOffset) & textureHeightMask;
                    int _x1 = ((int)(xMapPos) + textureInfo.XOffset) & textureWidthMask;
                    int textureIndex = _y1 * textureWidth + _x1;

                    ref BGRA tex = ref Unsafe.Add(ref ceilingTexturePtr, textureIndex);
                    ref BGRA screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);

                    ShadeByPrecalc(ref tex, ref screenTex, lightLevel);
                }

                renderWindow.CeilingStart = renderWindow.WallStart;
            }
        }

        [SkipLocalsInit]
        private void RenderCeilingVector(
            PortalPlayerSnapshot player,
            Sector sector,
            Span<BGRA> screen)
        {
            byte lightLevel = sector.LightLevel;

            int height = PixelHeight;
            int width = PixelWidth;
            int halfHeightInt = height / 2;
            float oneOverHeight = 1f / height;
            int widthDiv2 = width / 2;

            float px = player.X;
            float py = player.Y;
            float pz = player.Z;
            float pSin = player.Sin;
            float pCos = player.Cos;

            float xPosIncr = 1f / (width * -EngineConstants.HeightToWidthRatio);

            float yaw = player.Yaw;
            float yCeil = sector.Ceil - pz;

            Vector<float> pxV = Vector.Create(px);
            Vector<float> pyV = Vector.Create(py);
            Vector<float> pSinV = Vector.Create(pSin);
            Vector<float> pCosV = Vector.Create(pCos);
            Vector<float> yawV = Vector.Create(yaw);

            Vector<float> yCeilV = Vector.Create(yCeil);
            Vector<float> oneOverHeightV = Vector.Create(oneOverHeight);
            Vector<float> ivIncrF = new(oneOverHeight * Vector<float>.Count);

            Vector<float> incramentVector = default;
            ref float incramentVectorPtr = ref Unsafe.As<Vector<float>, float>(ref incramentVector);

            TextureInfo textureInfo = sector.CeilTexture;
            ref Texture ceilingTexture = ref TextureCache.GetTexture(textureInfo.Name);

            int textureHeight = ceilingTexture.Height;
            int textureWidth = ceilingTexture.Width;
            int textureHeightMask = ceilingTexture.Height - 1;
            int textureWidthMask = ceilingTexture.Width - 1;
            Vector<int> textureHeightMaskV = Vector.Create(textureHeightMask);
            Vector<int> textureWidthMaskV = Vector.Create(textureWidthMask);
            Vector<int> textureWidthV = Vector.Create(textureWidth);
            Vector<int> yOffSetV = Vector.Create(textureInfo.YOffset);
            Vector<int> xOffSetV = Vector.Create(textureInfo.XOffset);

            ref BGRA ceilingTexturePtr = ref MemoryMarshal.GetArrayDataReference(ceilingTexture.Data);
            ref BGRA screenPtr = ref MemoryMarshal.GetReference(screen);

            (int sectroFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            for (int x = sectroFromX; x <= sectorToX; x++)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.GetCeilingDimensions(x);

                if (Unsafe.IsNullRef(ref renderWindow) || renderWindow.CeilingStart >= renderWindow.WallStart)
                {
                    continue;
                }

                int floorFromY = renderWindow.CeilingStart;
                int floorToY = Math.Clamp(renderWindow.WallStart, renderWindow.CeilingStart, renderWindow.FloorEnd);

                int screenIndex = floorFromY * width + x;

                float xMapPosMultiplier = (widthDiv2 - x) * xPosIncr;

                Vector<float> xMapPosMultiplierV = Vector.Create(xMapPosMultiplier);
                Vector<int> halfHeightIntV = Vector.Create(halfHeightInt);

                for (int j = 0; j < Vector<int>.Count; j++)
                {
                    Unsafe.Add(ref incramentVectorPtr, j) = halfHeightInt - floorFromY - j;
                }

                incramentVector = Vector.FusedMultiplyAdd(incramentVector, oneOverHeightV, yawV);

                int rem = (floorToY - floorFromY) % Vector<int>.Count;
                floorToY -= rem;

                ref BGRA screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);
                ref BGRA toScalePtr = ref Unsafe.Add(ref screenPtr, floorToY * width + x);

                while (!Unsafe.AreSame(ref screenTex, ref toScalePtr))
                {
                    Vector<float> yMapPosR = yCeilV / incramentVector;
                    Vector<float> xMapPosR = yMapPosR * xMapPosMultiplierV;

                    (Vector<float> xMapPos, Vector<float> yMapPos) = RotateVertexBack(xMapPosR, yMapPosR, pSinV, pCosV, pxV, pyV);

                    Vector<int> _y1 = (Vector.ConvertToInt32Native(yMapPos) + yOffSetV) & textureHeightMaskV;
                    Vector<int> _x1 = (Vector.ConvertToInt32Native(xMapPos) + xOffSetV) & textureWidthMaskV;
                    Vector<int> textureIndex = _y1 * textureWidthV + _x1;

                    ref int textureIndexPtr = ref Unsafe.As<Vector<int>, int>(ref textureIndex);

                    for (int i = 0; i < Vector<int>.Count; i++, screenTex = ref Unsafe.Add(ref screenTex, width))
                    {
                        ref BGRA tex = ref Unsafe.Add(ref ceilingTexturePtr, Unsafe.Add(ref textureIndexPtr, i));
                        ShadeByPrecalc(ref tex, ref screenTex, lightLevel);
                    }

                    incramentVector -= ivIncrF;
                }

                float ii = incramentVectorPtr;

                for (int j = 0; j < rem; j++, screenTex = ref Unsafe.Add(ref screenTex, width), ii -= oneOverHeight)
                {
                    float yMapPosR = yCeil / (ii + yaw);
                    float xMapPosR = yMapPosR * xMapPosMultiplier;

                    (float xMapPos, float yMapPos) = RotateVertexBack(xMapPosR, yMapPosR, pSin, pCos, px, py);

                    int _y1 = ((int)(yMapPos) + textureInfo.YOffset) & textureHeightMask;
                    int _x1 = ((int)(xMapPos) + textureInfo.XOffset) & textureWidthMask;
                    int textureIndex = _y1 * textureWidth + _x1;

                    ref BGRA tex = ref Unsafe.Add(ref ceilingTexturePtr, textureIndex);
                    ShadeByPrecalc(ref tex, ref screenTex, lightLevel);
                }

                renderWindow.CeilingStart = renderWindow.WallStart;
            }
        }

        private void RenderSkybox(
            PortalPlayerSnapshot player,
            Span<BGRA> screen,
            ref Texture ceilingTexture
            )
        {
            const float twoPi = 2 * MathF.PI;
            const float oneOverTwoPi = 1f / (2 * MathF.PI);

            int width = PixelWidth;
            int height = PixelHeight;
            float viewAngle = player.Angle;

            ref BGRA ceilingTexturePtr = ref MemoryMarshal.GetArrayDataReference(ceilingTexture.Data);
            ref BGRA screenPtr = ref MemoryMarshal.GetReference(screen);
            ref float angleCachePtr = ref MemoryMarshal.GetArrayDataReference(angleCache);

            (int sectroFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            int textureWidth = ceilingTexture.Width;
            int textureHeight = ceilingTexture.Height;

            float textureWidth4 = textureWidth * 4f * oneOverTwoPi;
            float yTextureIncr = (1f / height) * textureHeight;

            for (int x = sectroFromX; x <= sectorToX; x++)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.GetCeilingDimensions(x);

                if (Unsafe.IsNullRef(ref renderWindow) || renderWindow.CeilingStart >= renderWindow.WallStart)
                {
                    continue;
                }

                // calculate angle between 0 to 2 PI
                float angleX = Unsafe.Add(ref angleCachePtr, x) - viewAngle;
                if (angleX > twoPi)
                {
                    angleX = angleX - twoPi;
                }
                else if (angleX < 0f)
                {
                    angleX = twoPi + angleX;
                }

                int texX = (int)(textureWidth4 * angleX) % textureWidth;

                int ceilingStart = renderWindow.CeilingStart;
                int wallStartClamped = Math.Clamp(renderWindow.WallStart, renderWindow.CeilingStart, renderWindow.FloorEnd);

                float vScreen = (float)ceilingStart * yTextureIncr;

                ref BGRA screenColumnPtr = ref Unsafe.Add(ref screenPtr, x + width * ceilingStart);
                ref BGRA textureColumnPtr = ref Unsafe.Add(ref ceilingTexturePtr, texX);

                for (int y = ceilingStart; y < wallStartClamped; y++, vScreen += yTextureIncr)
                {
                    int index = ((int)vScreen) % textureHeight;

                    screenColumnPtr = Unsafe.Add(ref textureColumnPtr, index);
                    screenColumnPtr = ref Unsafe.Add(ref screenColumnPtr, width);
                }

                renderWindow.CeilingStart = renderWindow.WallStart;
            }

        }

        private void RenderSkyboxVector(
            PortalPlayerSnapshot player,
            Span<BGRA> screen,
            ref Texture ceilingTexture
            )
        {
            const float twoPi = 2 * MathF.PI;
            const float oneOverTwoPi = 1f / (2 * MathF.PI);

            int width = PixelWidth;
            int height = PixelHeight;
            float viewAngle = player.Angle;

            ref BGRA ceilingTexturePtr = ref MemoryMarshal.GetArrayDataReference(ceilingTexture.Data);
            ref BGRA screenPtr = ref MemoryMarshal.GetReference(screen);
            ref float angleCachePtr = ref MemoryMarshal.GetArrayDataReference(angleCache);

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            int textureWidth = ceilingTexture.Width;
            int textureHeight = ceilingTexture.Height;

            float textureWidth4 = textureWidth * 4f * oneOverTwoPi;
            float yTextureIncr = (1f / height) * textureHeight;

            Vector<int> zeroV = Vector.Create(0);
            Vector<int> textureWidthV = Vector.Create(ceilingTexture.Width);

            Vector<float> ivIncrF = new(yTextureIncr * Vector<float>.Count);

            Vector<float> incramentVector = Vector.CreateSequence(0f, yTextureIncr);

            for (int x = sectorFromX; x <= sectorToX; x++)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.GetCeilingDimensions(x);

                if (Unsafe.IsNullRef(ref renderWindow) || renderWindow.CeilingStart >= renderWindow.WallStart)
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

                int texX = (int)(textureWidth4 * angleX) % textureWidth;

                int ceilingStart = renderWindow.CeilingStart;
                int wallStartClamped = Math.Clamp(renderWindow.WallStart, renderWindow.CeilingStart, renderWindow.FloorEnd);

                int rem = (wallStartClamped - ceilingStart) % Vector<int>.Count;
                wallStartClamped -= rem;

                ref BGRA screenColumnPtr = ref Unsafe.Add(ref screenPtr, x + width * ceilingStart);
                ref BGRA screenEndColumnPtr = ref Unsafe.Add(ref screenPtr, x + width * wallStartClamped);
                ref BGRA textureColumnPtr = ref Unsafe.Add(ref ceilingTexturePtr, texX);

                float vScreen = ceilingStart * yTextureIncr;
                Vector<float> vScreenV = Vector.Create(vScreen) + incramentVector;

                for (; !Unsafe.AreSame(ref screenColumnPtr, ref screenEndColumnPtr); vScreenV += ivIncrF)
                {
                    Vector<int> texY = Vector.ConvertToInt32Native(vScreenV);

                    ref int texYPtr = ref Unsafe.As<Vector<int>, int>(ref texY);

                    for (int j = 0; j < Vector<int>.Count; j++)
                    {
                        int index = Unsafe.Add(ref texYPtr, j);

                        if (index >= textureHeight)
                        {
                            index -= textureHeight;
                        }

                        index *= textureWidth;

                        screenColumnPtr = Unsafe.Add(ref textureColumnPtr, index);
                        screenColumnPtr = ref Unsafe.Add(ref screenColumnPtr, width);
                    }
                }

                Vector<int> vScreenVInt = Vector.ConvertToInt32Native(vScreenV);

                for (int y = 0; y < rem; y++)
                {
                    int index = vScreenVInt[y];
                    if (index >= textureHeight)
                    {
                        index -= textureHeight;
                    }
                    index *= textureWidth;

                    screenColumnPtr = Unsafe.Add(ref textureColumnPtr, index);
                    screenColumnPtr = ref Unsafe.Add(ref screenColumnPtr, width);
                }

                renderWindow.CeilingStart = renderWindow.WallStart;
            }

        }

        [SkipLocalsInit]
        public void RenderFloor(
            PortalPlayerSnapshot player,
            Sector sector,
            Span<BGRA> screen)
        {
            byte lightLevel = sector.LightLevel;

            int height = PixelHeight;
            int width = PixelWidth;

            float px = player.X;
            float py = player.Y;
            float pz = player.Z;
            float pSin = player.Sin;
            float pCos = player.Cos;

            float yfloor = sector.Floor - pz;
            float yaw = player.Yaw;

            float xPosIncr = 1f / (width * -EngineConstants.HeightToWidthRatio);

            float oneOvervFov = 1f / height;
            int halfHeightInt = height / 2;

            TextureInfo textureInfo = sector.FloorTexture;
            ref Texture floorTexture = ref TextureCache.GetTexture(textureInfo.Name);

            ref BGRA floorTexturePtr = ref MemoryMarshal.GetArrayDataReference(floorTexture.Data);
            ref BGRA screenPtr = ref MemoryMarshal.GetReference(screen);

            int textureWidth = floorTexture.Width;
            int textureHeightMask = floorTexture.Height - 1;
            int textureWidthMask = floorTexture.Width - 1;

            (int sectroFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            for (int x = sectroFromX; x <= sectorToX; x++)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.GetFloorCeilDimensions2(x);

                if (Unsafe.IsNullRef(ref renderWindow) || renderWindow.CeilingStart >= renderWindow.FloorEnd || renderWindow.WallEnd >= renderWindow.FloorEnd)
                {
                    continue;
                }

                int floorFromY = renderWindow.WallEnd;
                int floorToY = renderWindow.FloorEnd;

                int screenIndex = floorFromY * width + x;
                float xMapPosMultiplier = (width / 2 - x) * xPosIncr;
                int increment = halfHeightInt - floorFromY;

                // from start of wall (buttom) to screen buttom
                for (int i = floorFromY; i < floorToY; i++, screenIndex += width)
                {
                    float yMapPosR = yfloor / (increment * oneOvervFov + yaw);
                    float xMapPosR = yMapPosR * xMapPosMultiplier;

                    (float xMapPos, float yMapPos) = RotateVertexBack(xMapPosR, yMapPosR, pSin, pCos, px, py);

                    int _y1 = ((int)(yMapPos) + textureInfo.YOffset) & textureHeightMask;
                    int _x1 = ((int)(xMapPos) + textureInfo.XOffset) & textureWidthMask;
                    int textureIndex = (_y1 * textureWidth) + _x1;

                    ref BGRA tex = ref Unsafe.Add(ref floorTexturePtr, textureIndex);
                    ref BGRA screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);
                    ShadeByPrecalc(ref tex, ref screenTex, lightLevel);

                    increment -= 1;
                }

                renderWindow.FloorEnd = renderWindow.WallEnd;
            }
        }

        [SkipLocalsInit]
        public void RenderFloorVector(
            PortalPlayerSnapshot player,
            Sector sector,
            Span<BGRA> screen)
        {
            byte lightLevel = sector.LightLevel;

            int height = PixelHeight;
            int width = PixelWidth;

            float px = player.X;
            float py = player.Y;
            float pz = player.Z;
            float pSin = player.Sin;
            float pCos = player.Cos;

            float yfloor = sector.Floor - pz;
            float yaw = player.Yaw;

            float xPosIncr = 1f / (width * -EngineConstants.HeightToWidthRatio);

            float oneOvervFov = 1f / height;
            int halfHeightInt = height / 2;
            int widthDiv2 = width / 2;

            TextureInfo textureInfo = sector.FloorTexture;
            ref Texture floorTexture = ref TextureCache.GetTexture(textureInfo.Name);

            ref BGRA floorTexturePtr = ref MemoryMarshal.GetArrayDataReference(floorTexture.Data);
            ref BGRA screenPtr = ref MemoryMarshal.GetReference(screen);

            Vector<float> pxV = Vector.Create(px);
            Vector<float> pyV = Vector.Create(py);
            Vector<float> pSinV = Vector.Create(pSin);
            Vector<float> pCosV = Vector.Create(pCos);
            Vector<float> yawV = Vector.Create(yaw);
            Vector<float> yfloorV = Vector.Create(yfloor);
            Vector<float> oneOvervFovV = Vector.Create(oneOvervFov);
            Vector<float> ivIncrF = new(oneOvervFov * Vector<float>.Count);
            Vector<int> xOffSetV = Vector.Create(textureInfo.XOffset);
            Vector<int> yOffSetV = Vector.Create(textureInfo.YOffset);

            int textureHeight = floorTexture.Height;
            int textureWidth = floorTexture.Width;
            int textureHeightMask = floorTexture.Height - 1;
            int textureWidthMask = floorTexture.Width - 1;

            Vector<int> textureHeightMaskV = Vector.Create(textureHeightMask);
            Vector<int> textureWidthMaskV = Vector.Create(textureWidthMask);
            Vector<int> textureWidthV = Vector.Create(textureWidth);

            Vector<float> incramentVector = default;
            ref float incramentVectorPtr = ref Unsafe.As<Vector<float>, float>(ref incramentVector);

            (int sectorFromX, int sectorToX) = this.RenderWindowHelper.GetSectorX();

            for (int x = sectorFromX; x <= sectorToX; x++)
            {
                ref RenderWindow renderWindow = ref RenderWindowHelper.GetFloorCeilDimensions2(x);

                if (Unsafe.IsNullRef(ref renderWindow) || renderWindow.CeilingStart >= renderWindow.FloorEnd || renderWindow.WallEnd >= renderWindow.FloorEnd)
                {
                    continue;
                }

                int floorFromY = Math.Clamp(renderWindow.WallEnd, renderWindow.CeilingStart, renderWindow.FloorEnd);
                int floorToY = renderWindow.FloorEnd;

                int screenIndex = floorFromY * width + x;
                float xMapPosMultiplier = (widthDiv2 - x) * xPosIncr;
                Vector<float> xMapPosMultiplierV = Vector.Create(xMapPosMultiplier);
                for (int j = 0; j < Vector<int>.Count; j++)
                {
                    Unsafe.Add(ref incramentVectorPtr, j) = halfHeightInt - floorFromY - j;
                }
                incramentVector = Vector.FusedMultiplyAdd(incramentVector, oneOvervFovV, yawV);

                int rem = (floorToY - floorFromY) % Vector<int>.Count;
                floorToY -= rem;

                ref BGRA screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);
                ref BGRA toScalePtr = ref Unsafe.Add(ref screenPtr, floorToY * width + x);

                // from start of wall (buttom) to screen buttom
                while (!Unsafe.AreSame(ref screenTex, ref toScalePtr))
                {
                    Vector<float> yMapPosR = yfloorV / incramentVector;
                    Vector<float> xMapPosR = yMapPosR * xMapPosMultiplierV; 

                    (Vector<float> xMapPos, Vector<float> yMapPos) = RotateVertexBack(xMapPosR, yMapPosR, pSinV, pCosV, pxV, pyV);

                    Vector<int> _y1 = (Vector.ConvertToInt32Native(yMapPos) + yOffSetV) & textureHeightMaskV;
                    Vector<int> _x1 = (Vector.ConvertToInt32Native(xMapPos) + xOffSetV) & textureWidthMaskV;
                    Vector<int> textureIndex = _y1 * textureWidthV + _x1;             

                    ref int textureIndexPtr = ref Unsafe.As<Vector<int>, int>(ref textureIndex);

                    for (int j = 0; j < Vector<float>.Count; j++, screenTex = ref Unsafe.Add(ref screenTex, width))
                    {
                        if (Unsafe.Add(ref incramentVectorPtr, j) == 0f)
                        {
                            continue;
                        }

                        ref BGRA tex = ref Unsafe.Add(ref floorTexturePtr, Unsafe.Add(ref textureIndexPtr, j));
                        ShadeByPrecalc(ref tex, ref screenTex, lightLevel);
                    }

                    incramentVector -= ivIncrF;
                }

                int increment = halfHeightInt - floorFromY - (floorToY - floorFromY);

                for (int i = 0; i < rem; i++, screenTex = ref Unsafe.Add(ref screenTex, width))
                {
                    float yMapPosR = yfloor / (increment * oneOvervFov + yaw);
                    float xMapPosR = yMapPosR * xMapPosMultiplier;

                    (float xMapPos, float yMapPos) = RotateVertexBack(xMapPosR, yMapPosR, pSin, pCos, px, py);

                    int _y1 = ((int)(yMapPos) + textureInfo.YOffset) & textureHeightMask;
                    int _x1 = ((int)(xMapPos) + textureInfo.XOffset) & textureWidthMask;
                    int textureIndex = (_y1 * textureWidth) + _x1;

                    ref BGRA tex = ref Unsafe.Add(ref floorTexturePtr, textureIndex);
                    ShadeByPrecalc(ref tex, ref screenTex, lightLevel);

                    increment -= 1;
                }

                renderWindow.FloorEnd = renderWindow.WallEnd;
            }
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
        private static (float rx1, float ry1) RotateVertexBack(
            float x, float y,
            float psin, float pcos,
            float px, float py)
        {
            float rx1 = y * pcos + x * psin;
            float ry1 = y * psin - x * pcos;

            return (rx1 + px, ry1 + py);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ShadeByPrecalc(ref BGRA inColor, ref BGRA outColor, uint scale)
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
