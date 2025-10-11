using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    internal sealed class WallComparer : IComparer<Wall>
    {
        private readonly float cameraWidthIncr;

        public WallComparer(int width) {
            this.cameraWidthIncr = 2.0f / width;
        }

        public int Compare(Wall? x, Wall? y)
        {
            ArgumentNullException.ThrowIfNull(x);
            ArgumentNullException.ThrowIfNull(y);

            return Compare3(x, y);
        }

        [SkipLocalsInit]
        private int Compare3(Wall x, Wall y)
        {
            // x = y are the same
            if (x.X1 == y.X1 && x.X2 == y.X2 && x.Y2 == y.Y2)
            {
                return 0;
            }

            float xCX1 = x.CX1;
            float xCX2 = x.CX2;
            float xCY1 = x.CY1;
            float xCY2 = x.CY2;

            float yCX1 = y.CX1;
            float yCX2 = y.CX2;
            float yCY1 = y.CY1;
            float yCY2 = y.CY2;

            if (Within(x.XLeft, y.XLeft, y.XRight))
            {
                // y left
                TryGetIntersection(x.XLeft, y.X1, y.Y1, y.X2, y.Y2, ref yCX1, ref yCY1);
            }

            if (Within(x.XRight, y.XLeft, y.XRight))
            {
                // y right
                TryGetIntersection(x.XRight, y.X1, y.Y1, y.X2, y.Y2, ref yCX2, ref yCY2);
            }

            if (Within(y.XLeft, x.XLeft, x.XRight))
            {
                // x left
                TryGetIntersection(y.XLeft, x.X1, x.Y1, x.X2, x.Y2, ref xCX1, ref xCY1);
            }

            if (Within(y.XRight, x.XLeft, x.XRight))
            {
                // x right
                TryGetIntersection(y.XRight, x.X1, x.Y1, x.X2, x.Y2, ref xCX2, ref xCY2);
            }

            // for connected walls, only compare the un-connected vertex
            bool connected1 = x.X1 == y.X1 && x.Y1 == y.Y1;
            bool connected2 = x.X2 == y.X2 && x.Y2 == y.Y2;
            bool connected3 = x.X1 == y.X2 && x.Y1 == y.Y2;
            bool connected4 = x.X2 == y.X1 && x.Y2 == y.Y1;

            if (connected1)
            {
                return Compare(xCX2, xCY2, yCX2, yCY2);
            }
            else if (connected2)
            {
                return Compare(xCX1, xCY1, yCX1, yCY1);

            }
            else if (connected3)
            {
                return Compare(xCX2, xCY2, yCX1, yCY1);
            }
            else if (connected4)
            {
                return Compare(xCX1, xCY1, yCX2, yCY2);
            }

            float xD1 = xCX1 * xCX1 + xCY1 * xCY1;
            float xD2 = xCX2 * xCX2 + xCY2 * xCY2;
            float yD1 = yCX1 * yCX1 + yCY1 * yCY1;
            float yD2 = yCX2 * yCX2 + yCY2 * yCY2;
            bool yd1Further = yD1 > xD1 && yD1 > xD2;
            bool yd2Further = yD2 > xD1 && yD2 > xD2;

            if (yd1Further && yd2Further)
            {
                return -1;
            }
            else if (!yd1Further && !yd2Further)
            {
                return 1;
            }

            float xd3 = xD1 + xD2;
            float yd3 = yD1 + yD2;

            if (yd3 == xd3)
            {
                return 0;
            }

            return yd3 > xd3 ? -1 : 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool Within(int value, int from, int to)
            {
                return value > from && value < to;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static int Compare(float xcx, float xcy, float ycx, float ycy)
            {
                float xd = xcx * xcx + xcy * xcy;
                float yd = ycx * ycx + ycy * ycy;

                if (xd == yd)
                {
                    return 0;
                }

                return xd < yd ? -1 : 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryGetIntersection(
            int x,
            float rx1, float ry1,
            float rx2, float ry2,
            ref float cx,
            ref float cy)
        {
            float rayDirX = EngineConstants.CameraPlaneX * ((cameraWidthIncr * x) - 1f);
            float d2x = rx2 - rx1;
            float d2y = ry2 - ry1;

            float denominator = rayDirX * d2y - d2x;

            if (MathF.Abs(denominator) < float.Epsilon)
            {
                return;
            }

            float u = (rx1 - ry1 * rayDirX) / denominator;

            if (u < 0f || u > 1f)
            {
                return;
            }

            float t = (rx1 * d2y - ry1 * d2x) / denominator;

            if (t < 0f)
            {
                return;
            }

            cy = t;
            cx = t * rayDirX;
        }
    }
}
