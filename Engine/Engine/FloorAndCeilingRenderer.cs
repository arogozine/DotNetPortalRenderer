using RenderingEngine.Models;
using RenderingEngine.TextureManagement;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RenderingEngine.Engine
{
    internal sealed partial class PortalRenderer
    {
        private static readonly Vector<float> sixtyFourF = Vector.Create(64f);
        private static readonly Vector<int> sixtyFour = Vector.Create(64);

        private void RenderCeiling(
            PortalPlayerSnapshot player,
            Sector sector,
            Span<BGRA> screen,
            TextureInfo ceilingTexture)
        {
            int height = PixelHeight;
            int width = PixelWidth;
            float vFov = VFov;
            int halfHeightInt = height / 2;
            float oneOvervFov = 1f / vFov;

            (float px, float py, float pz) = player.Where;
            (float pSin, float pCos) = MathF.SinCos(player.Angle);

            float idkWhatThisIs = 1f / (width * -0.7594506f);

            float yaw = player.Yaw;
            float yCeil = sector.Ceil - pz;

            const float sixtyFourF = 64f;

            ref BGRA ceilingTexturePtr = ref ceilingTexture.Texture;
            ref BGRA screenPtr = ref MemoryMarshal.GetReference(screen);
            ref uint scalePtr = ref MemoryMarshal.GetArrayDataReference(distanceMult);

            for (int x = 0; x < PixelWidth; x++)
            {
                var info = RenderWindowHelper.GetFloorCeilDimensions2(x);

                if (!info.Calculated || info.CeilingStart >= info.WallStart)
                {
                    continue;
                }

                int floorFromY = info.CeilingStart;
                int floorToY = info.WallStart;

                int screenIndex = floorFromY * width + x;

                float xMapPosMultiplier = (width / 2 - x) * idkWhatThisIs;

                int ii = halfHeightInt - floorFromY;

                // from screen top to start of the wall (bottom)
                for (int j = floorFromY; j < floorToY; j++, screenIndex += width)
                {
                    float yMapPosR = yCeil / (ii * oneOvervFov + yaw);
                    float xMapPosR = yMapPosR * xMapPosMultiplier;

                    (float xMapPos, float yMapPos) = RotateVertexBack(xMapPosR, yMapPosR, pSin, pCos, px, py);

                    float xf = MathF.Abs(xMapPos - MathF.Truncate(xMapPos));
                    float yf = MathF.Abs(yMapPos - MathF.Truncate(yMapPos));

                    int _y1 = (int)(yf * sixtyFourF);
                    int _x1 = (int)(xf * sixtyFourF);
                    int textureIndex = (_y1 << 6) + _x1;

                    ref BGRA tex = ref Unsafe.Add(ref ceilingTexturePtr, textureIndex);
                    ref BGRA screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);
                    ShadeByPrecalc(ref tex, ref screenTex, ref Unsafe.Add(ref scalePtr, j));

                    ii--;
                }

                ref RenderWindow renderWindow = ref RenderWindowHelper.RenderWindow[x];
                renderWindow.CeilingStart = renderWindow.WallStart;
            }
        }

        private void RenderCeilingVector(
            PortalPlayerSnapshot player,
            Sector sector,
            Span<BGRA> screen,
            TextureInfo ceilingTexture
            )
        {
            int height = PixelHeight;
            int width = PixelWidth;
            float vFov = VFov;
            int halfHeightInt = height / 2;
            float oneOvervFov = 1f / vFov;
            int widthDiv2 = width / 2;

            (float px, float py, float pz) = player.Where;
            (float pSin, float pCos) = MathF.SinCos(player.Angle);

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

            ref BGRA ceilingTexturePtr = ref ceilingTexture.Texture;
            ref BGRA screenPtr = ref MemoryMarshal.GetReference(screen);
            ref uint scalePtr = ref MemoryMarshal.GetArrayDataReference(distanceMult);

            for (int x = 0; x < PixelWidth; x++)
            {
                var info = RenderWindowHelper.GetFloorCeilDimensions2(x);

                if (!info.Calculated || info.CeilingStart >= info.WallStart)
                {
                    continue;
                }

                int floorFromY = info.CeilingStart;
                int floorToY = info.WallStart;

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

                    Vector<float> xf = Vector.Abs(xMapPos - Vector.Truncate(xMapPos));
                    Vector<float> yf = Vector.Abs(yMapPos - Vector.Truncate(yMapPos));

                    Vector<int> _y1 = Vector.ConvertToInt32Native(yf * sixtyFourF);
                    Vector<int> _x1 = Vector.ConvertToInt32Native(xf * sixtyFourF);
                    Vector<int> textureIndex = _y1 * sixtyFour + _x1; // Vector.ShiftLeft(_y1, 6) + _x1;

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

                    float xf = MathF.Abs(xMapPos - MathF.Truncate(xMapPos));
                    float yf = MathF.Abs(yMapPos - MathF.Truncate(yMapPos));

                    int _y1 = (int)(yf * 64f);
                    int _x1 = (int)(xf * 64f);
                    int textureIndex = (_y1 << 6) + _x1;

                    ref BGRA tex = ref Unsafe.Add(ref ceilingTexturePtr, textureIndex);
                    ref BGRA screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);
                    ShadeByPrecalc(ref tex, ref screenTex, ref Unsafe.Add(ref scalePtr, j));

                    ii -= oneOvervFov; // --;
                }

                ref RenderWindow renderWindow = ref RenderWindowHelper.RenderWindow[x];
                renderWindow.CeilingStart = renderWindow.WallStart;
            }
        }


        public void RenderFloor(
            PortalPlayerSnapshot player,
            Sector sector,
            Span<BGRA> screen,
            TextureInfo floorTexture)
        {
            int height = PixelHeight;
            int width = PixelWidth;
            float vFov = VFov;

            (float pSin, float pCos) = MathF.SinCos(player.Angle);
            (float px, float py, float pz) = player.Where;
            float yfloor = sector.Floor - pz;
            float yaw = player.Yaw;

            float idkWhatThisIs = 1f / (width * -0.7594506f);

            float oneOvervFov = 1f / vFov;
            int halfHeightInt = height / 2;

            ref BGRA floorTexturePtr = ref floorTexture.Texture;
            ref BGRA screenPtr = ref MemoryMarshal.GetReference(screen);
            ref uint scalePtr = ref MemoryMarshal.GetArrayDataReference(distanceMult);

            const float sixtyFourF = 64f;

            for (int x = 0; x < PixelWidth; x++)
            {
                var info = RenderWindowHelper.GetFloorCeilDimensions2(x);

                if (!info.Calculated || info.CeilingStart >= info.FloorEnd || info.WallEnd >= info.FloorEnd)
                {
                    continue;
                }

                int floorFromY = info.WallEnd;
                int floorToY = info.FloorEnd;

                int screenIndex = floorFromY * width + x;
                float xMapPosMultiplier = (width / 2 - x) * idkWhatThisIs;
                int increment = halfHeightInt - floorFromY;

                // from start of wall (buttom) to screen buttom
                for (int i = floorFromY; i < floorToY; i++, screenIndex += width)
                {
                    float yMapPosR = yfloor / (increment * oneOvervFov + yaw);
                    float xMapPosR = yMapPosR * xMapPosMultiplier;

                    (float xMapPos, float yMapPos) = RotateVertexBack(xMapPosR, yMapPosR, pSin, pCos, px, py);

                    float xf = MathF.Abs(xMapPos - MathF.Truncate(xMapPos));
                    float yf = MathF.Abs(yMapPos - MathF.Truncate(yMapPos));

                    int _y1 = (int)(yf * sixtyFourF);
                    int _x1 = (int)(xf * sixtyFourF);
                    int textureIndex = (_y1 << 6) + _x1;

                    ref BGRA tex = ref Unsafe.Add(ref floorTexturePtr, textureIndex);
                    ref BGRA screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);
                    ShadeByPrecalc(ref tex, ref screenTex, ref Unsafe.Add(ref scalePtr, i));

                    increment -= 1;
                }

                ref RenderWindow renderWindow = ref RenderWindowHelper.RenderWindow[x];
                renderWindow.FloorEnd = renderWindow.WallEnd;
            }
        }

        public void RenderFloorVector(
            PortalPlayerSnapshot player,
            Sector sector,
            Span<BGRA> screen,
            TextureInfo floorTexture)
        {
            int height = PixelHeight;
            int width = PixelWidth;
            float vFov = VFov;

            (float pSin, float pCos) = MathF.SinCos(player.Angle);
            (float px, float py, float pz) = player.Where;
            float yfloor = sector.Floor - pz;
            float yaw = player.Yaw;

            float idkWhatThisIs = 1f / (width * -0.7594506f);

            float oneOvervFov = 1f / vFov;
            int halfHeightInt = height / 2;

            ref BGRA floorTexturePtr = ref floorTexture.Texture;
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

            for (int x = 0; x < PixelWidth; x++)
            {
                var info = RenderWindowHelper.GetFloorCeilDimensions2(x);

                if (!info.Calculated || info.CeilingStart >= info.FloorEnd || info.WallEnd >= info.FloorEnd)
                {
                    continue;
                }

                int floorFromY = info.WallEnd;
                int floorToY = info.FloorEnd;

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

                    Vector<float> xf = Vector.Abs(xMapPos - Vector.Truncate(xMapPos));
                    Vector<float> yf = Vector.Abs(yMapPos - Vector.Truncate(yMapPos));

                    Vector<int> _y1 = Vector.ConvertToInt32Native(yf * sixtyFourF);
                    Vector<int> _x1 = Vector.ConvertToInt32Native(xf * sixtyFourF);
                    Vector<int> textureIndex = _y1 * sixtyFour + _x1; // Vector.ShiftLeft(_y1, 6) + _x1;

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

                    float xf = MathF.Abs(xMapPos - MathF.Truncate(xMapPos));
                    float yf = MathF.Abs(yMapPos - MathF.Truncate(yMapPos));

                    int _y1 = (int)(yf * 64f);
                    int _x1 = (int)(xf * 64f);
                    int textureIndex = (_y1 << 6) + _x1;

                    ref BGRA tex = ref Unsafe.Add(ref floorTexturePtr, textureIndex);
                    ref BGRA screenTex = ref Unsafe.Add(ref screenPtr, screenIndex);
                    ShadeByPrecalc(ref tex, ref screenTex, ref Unsafe.Add(ref scalePtr, i));

                    increment -= 1;
                }

                ref RenderWindow renderWindow = ref RenderWindowHelper.RenderWindow[x];
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
