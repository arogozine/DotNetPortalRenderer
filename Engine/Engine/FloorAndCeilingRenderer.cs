using RenderingEngine.Models;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private static readonly Vector<int> sixtyThree = Vector.Create(
            0b111111 // 63
        );
        private static readonly Vector<int> sixtyFour = Vector.Create(64);

        [SkipLocalsInit]
        private void RenderCeiling(
            PortalPlayerSnapshot player,
            Sector sector,
            Span<BGRA> screen,
            ref Texture ceilingTexture)
        {
            int height = PixelHeight;
            int width = PixelWidth;
            float vFov = VFov;
            int halfHeightInt = height / 2;
            float oneOvervFov = 1f / vFov;

            float px = player.X;
            float py = player.Y;
            float pz = player.Z;
            float pSin = player.Sin;
            float pCos = player.Cos;

            float idkWhatThisIs = 1f / (width * -0.7594506f);

            float yaw = player.Yaw;
            float yCeil = sector.Ceil - pz;

            ref BGRA ceilingTexturePtr = ref MemoryMarshal.GetArrayDataReference(ceilingTexture.Data);
            ref BGRA screenPtr = ref MemoryMarshal.GetReference(screen);
            ref uint scalePtr = ref MemoryMarshal.GetArrayDataReference(distanceMult);

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

                float xMapPosMultiplier = (width / 2 - x) * idkWhatThisIs;

                int ii = halfHeightInt - floorFromY;

                // from screen top to start of the wall (bottom)
                for (int j = floorFromY; j < floorToY; j++, screenIndex += width)
                {
                    float yMapPosR = yCeil / (ii * oneOvervFov + yaw);
                    float xMapPosR = yMapPosR * xMapPosMultiplier;

                    (float xMapPos, float yMapPos) = RotateVertexBack(xMapPosR, yMapPosR, pSin, pCos, px, py);

                    int _y1 = (int)(yMapPos) & 63;
                    int _x1 = (int)(xMapPos) & 63;
                    int textureIndex = (_y1 << 6) + _x1;

                    ref BGRA tex = ref Unsafe.Add(ref ceilingTexturePtr, textureIndex);
                    ref BGRA screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);
                    ShadeByPrecalc(ref tex, ref screenTex, ref Unsafe.Add(ref scalePtr, j));

                    ii--;
                }

                renderWindow.CeilingStart = renderWindow.WallStart;
            }
        }

        [SkipLocalsInit]
        private void RenderCeilingVector(
            PortalPlayerSnapshot player,
            Sector sector,
            Span<BGRA> screen,
            ref Texture ceilingTexture
            )
        {
            int height = PixelHeight;
            int width = PixelWidth;
            float vFov = VFov;
            int halfHeightInt = height / 2;
            float oneOvervFov = 1f / vFov;
            int widthDiv2 = width / 2;

            float px = player.X;
            float py = player.Y;
            float pz = player.Z;
            float pSin = player.Sin;
            float pCos = player.Cos;

            float idkWhatThisIs = 1f / (width * -0.7594506f);

            float yaw = player.Yaw;
            float yCeil = sector.Ceil - pz;

            Vector<float> pxV = Vector.Create(px);
            Vector<float> pyV = Vector.Create(py);
            Vector<float> pSinV = Vector.Create(pSin);
            Vector<float> pCosV = Vector.Create(pCos);
            Vector<float> yawV = Vector.Create(yaw);

            Vector<float> yCeilV = Vector.Create(yCeil);
            Vector<float> oneOvervFovV = Vector.Create(oneOvervFov);
            Vector<float> ivIncrF = new(oneOvervFov * Vector<float>.Count);

            Vector<float> incramentVector = default;
            ref float incramentVectorPtr = ref Unsafe.As<Vector<float>, float>(ref incramentVector);

            ref BGRA ceilingTexturePtr = ref MemoryMarshal.GetArrayDataReference(ceilingTexture.Data);
            ref BGRA screenPtr = ref MemoryMarshal.GetReference(screen);
            ref uint scalePtr = ref MemoryMarshal.GetArrayDataReference(distanceMult);

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

                float xMapPosMultiplier = (width / 2 - x) * idkWhatThisIs;

                Vector<float> xMapPosMultiplierV = Vector.Create(xMapPosMultiplier);
                Vector<int> halfHeightIntV = Vector.Create(halfHeightInt);

                for (int j = 0; j < Vector<int>.Count; j++)
                {
                    Unsafe.Add(ref incramentVectorPtr, j) = (halfHeightInt - floorFromY - j) * oneOvervFov + yaw;
                }

                int rem = (floorToY - floorFromY) % Vector<int>.Count;
                floorToY -= rem;

                ref uint fromScalePtr = ref Unsafe.Add(ref scalePtr, floorFromY);
                ref uint toScalePtr = ref Unsafe.Add(ref scalePtr, floorToY);

                while (!Unsafe.AreSame(ref fromScalePtr, ref toScalePtr))
                {
                    Vector<float> yMapPosR = yCeilV / incramentVector;
                    Vector<float> xMapPosR = yMapPosR * xMapPosMultiplierV;

                    (Vector<float> xMapPos, Vector<float> yMapPos) = RotateVertexBack(xMapPosR, yMapPosR, pSinV, pCosV, pxV, pyV);

                    Vector<int> _y1 = Vector.ConvertToInt32Native(yMapPos) & sixtyThree;
                    Vector<int> _x1 = Vector.ConvertToInt32Native(xMapPos) & sixtyThree;
                    Vector<int> textureIndex = _y1 * sixtyFour + _x1;

                    ref int textureIndexPtr = ref Unsafe.As<Vector<int>, int>(ref textureIndex);

                    for (int i = 0; i < Vector<int>.Count; i++, screenIndex += width)
                    {
                        ref BGRA tex = ref Unsafe.Add(ref ceilingTexturePtr, Unsafe.Add(ref textureIndexPtr, i));
                        ref BGRA screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);

                        fromScalePtr = ref Unsafe.Add(ref fromScalePtr, 1);
                        ShadeByPrecalc(ref tex, ref screenTex, ref fromScalePtr);
                    }

                    incramentVector -= ivIncrF;
                }

                floorFromY = floorToY;
                floorToY += rem;

                float ii = incramentVector[0];

                for (int j = floorFromY; j < floorToY; j++, screenIndex += width)
                {
                    float yMapPosR = yCeil / (ii + yaw); // (ii * oneOvervFov + yaw);
                    float xMapPosR = yMapPosR * xMapPosMultiplier;

                    (float xMapPos, float yMapPos) = RotateVertexBack(xMapPosR, yMapPosR, pSin, pCos, px, py);

                    int _y1 = (int)(yMapPos) & 63;
                    int _x1 = (int)(xMapPos) & 63;
                    int textureIndex = (_y1 << 6) + _x1;

                    ref BGRA tex = ref Unsafe.Add(ref ceilingTexturePtr, textureIndex);
                    ref BGRA screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);
                    ShadeByPrecalc(ref tex, ref screenTex, ref Unsafe.Add(ref scalePtr, j));

                    ii -= oneOvervFov; // --;
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

            int doubleTextureHeight = ceilingTexture.Height * 2 - 1;
            float textureWidth4 = textureWidth * 4f * oneOverTwoPi;
            float yTextureIncr = (2f / height) * textureHeight;

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
                    int index = Math.Clamp((int)(vScreen), 0, doubleTextureHeight);
                    index = index % textureHeight;
                    index *= textureWidth;

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
            float yTextureIncr = (2f / height) * textureHeight;
            int doubleTextureHeight = ceilingTexture.Height * 2 - 1;

            Vector<int> zeroV = Vector.Create(0);
            Vector<int> textureWidthV = Vector.Create(ceilingTexture.Width);
            Vector<int> textureHeightV = Vector.Create(doubleTextureHeight);

            Vector<float> ivIncrF = new(yTextureIncr * Vector<float>.Count);

            Vector<float> incramentVector = default;
            ref float incramentVectorPtr = ref Unsafe.As<Vector<float>, float>(ref incramentVector);
            float yTextureIncrSum = 0f;
            for (int j = 0; j < Vector<int>.Count; j++)
            {
                Unsafe.Add(ref incramentVectorPtr, j) = yTextureIncrSum;
                yTextureIncrSum += yTextureIncr;
            }

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
                    var texY = Vector.ClampNative(Vector.ConvertToInt32Native(vScreenV), zeroV, textureHeightV);

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
                    int index = Math.Clamp(vScreenVInt[y], 0, doubleTextureHeight);
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
            Span<BGRA> screen,
            ref Texture floorTexture)
        {
            int height = PixelHeight;
            int width = PixelWidth;
            float vFov = VFov;

            float px = player.X;
            float py = player.Y;
            float pz = player.Z;
            float pSin = player.Sin;
            float pCos = player.Cos;

            float yfloor = sector.Floor - pz;
            float yaw = player.Yaw;

            float idkWhatThisIs = 1f / (width * -0.7594506f);

            float oneOvervFov = 1f / vFov;
            int halfHeightInt = height / 2;

            ref BGRA floorTexturePtr = ref MemoryMarshal.GetArrayDataReference(floorTexture.Data);
            ref BGRA screenPtr = ref MemoryMarshal.GetReference(screen);
            ref uint scalePtr = ref MemoryMarshal.GetArrayDataReference(distanceMult);

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
                float xMapPosMultiplier = (width / 2 - x) * idkWhatThisIs;
                int increment = halfHeightInt - floorFromY;

                // from start of wall (buttom) to screen buttom
                for (int i = floorFromY; i < floorToY; i++, screenIndex += width)
                {
                    float yMapPosR = yfloor / (increment * oneOvervFov + yaw);
                    float xMapPosR = yMapPosR * xMapPosMultiplier;

                    (float xMapPos, float yMapPos) = RotateVertexBack(xMapPosR, yMapPosR, pSin, pCos, px, py);

                    int _y1 = (int)(yMapPos) & 63;
                    int _x1 = (int)(xMapPos) & 63;
                    int textureIndex = (_y1 << 6) + _x1;

                    ref BGRA tex = ref Unsafe.Add(ref floorTexturePtr, textureIndex);
                    ref BGRA screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);
                    ShadeByPrecalc(ref tex, ref screenTex, ref Unsafe.Add(ref scalePtr, i));

                    increment -= 1;
                }

                renderWindow.FloorEnd = renderWindow.WallEnd;
            }
        }

        [SkipLocalsInit]
        public void RenderFloorVector(
            PortalPlayerSnapshot player,
            Sector sector,
            Span<BGRA> screen,
            ref Texture floorTexture)
        {
            int height = PixelHeight;
            int width = PixelWidth;
            float vFov = VFov;

            float px = player.X;
            float py = player.Y;
            float pz = player.Z;
            float pSin = player.Sin;
            float pCos = player.Cos;

            float yfloor = sector.Floor - pz;
            float yaw = player.Yaw;

            float idkWhatThisIs = 1f / (width * -0.7594506f);

            float oneOvervFov = 1f / vFov;
            int halfHeightInt = height / 2;

            ref BGRA floorTexturePtr = ref MemoryMarshal.GetArrayDataReference(floorTexture.Data);
            ref BGRA screenPtr = ref MemoryMarshal.GetReference(screen);
            ref uint scalePtr = ref MemoryMarshal.GetArrayDataReference(distanceMult);

            Vector<float> pxV = Vector.Create(px);
            Vector<float> pyV = Vector.Create(py);
            Vector<float> pSinV = Vector.Create(pSin);
            Vector<float> pCosV = Vector.Create(pCos);
            Vector<float> yawV = Vector.Create(yaw);
            Vector<float> yfloorV = Vector.Create(yfloor);
            Vector<float> oneOvervFovV = Vector.Create(oneOvervFov);
            Vector<float> ivIncrF = new(oneOvervFov * Vector<float>.Count);

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
                float xMapPosMultiplier = (width / 2 - x) * idkWhatThisIs;
                Vector<float> xMapPosMultiplierV = Vector.Create(xMapPosMultiplier);

                for (int j = 0; j < Vector<int>.Count; j++)
                {
                    Unsafe.Add(ref incramentVectorPtr, j) = (halfHeightInt - floorFromY - j) * oneOvervFov + yaw;
                }

                int rem = (floorToY - floorFromY) % Vector<int>.Count;
                floorToY -= rem;

                ref uint fromScalePtr = ref Unsafe.Add(ref scalePtr, floorFromY);
                ref uint toScalePtr = ref Unsafe.Add(ref scalePtr, floorToY);

                // from start of wall (buttom) to screen buttom
                while (!Unsafe.AreSame(ref fromScalePtr, ref toScalePtr))
                {
                    Vector<float> yMapPosR = yfloorV / incramentVector;
                    Vector<float> xMapPosR = yMapPosR * xMapPosMultiplierV;

                    (Vector<float> xMapPos, Vector<float> yMapPos) = RotateVertexBack(xMapPosR, yMapPosR, pSinV, pCosV, pxV, pyV);

                    Vector<int> _y1 = Vector.ConvertToInt32Native(yMapPos) & sixtyThree;
                    Vector<int> _x1 = Vector.ConvertToInt32Native(xMapPos) & sixtyThree;
                    Vector<int> textureIndex = _y1 * sixtyFour + _x1;

                    ref int textureIndexPtr = ref Unsafe.As<Vector<int>, int>(ref textureIndex);

                    for (int j = 0; j < Vector<float>.Count; j++, screenIndex += width)
                    {
                        if (Unsafe.Add(ref incramentVectorPtr, j) == 0f)
                        {
                            fromScalePtr = ref Unsafe.Add(ref fromScalePtr, 1);
                            continue;
                        }

                        ref BGRA tex = ref Unsafe.Add(ref floorTexturePtr, Unsafe.Add(ref textureIndexPtr, j));
                        ref BGRA screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);

                        fromScalePtr = ref Unsafe.Add(ref fromScalePtr, 1);
                        ShadeByPrecalc(ref tex, ref screenTex, ref fromScalePtr);
                    }

                    incramentVector -= ivIncrF;
                }

                int increment = halfHeightInt - floorFromY - (floorToY - floorFromY);
                floorFromY = floorToY;
                floorToY += rem;

                for (int i = floorFromY; i < floorToY; i++, screenIndex += width)
                {
                    float yMapPosR = yfloor / (increment * oneOvervFov + yaw);
                    float xMapPosR = yMapPosR * xMapPosMultiplier;

                    (float xMapPos, float yMapPos) = RotateVertexBack(xMapPosR, yMapPosR, pSin, pCos, px, py);

                    int _y1 = (int)(yMapPos) & 63;
                    int _x1 = (int)(xMapPos) & 63;
                    int textureIndex = (_y1 << 6) + _x1;

                    ref BGRA tex = ref Unsafe.Add(ref floorTexturePtr, textureIndex);
                    ref BGRA screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);
                    ShadeByPrecalc(ref tex, ref screenTex, ref Unsafe.Add(ref scalePtr, i));

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
    }
}
