using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    /// <summary>
    /// Wall distance comparer
    /// </summary>
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
            if (x.R1 == y.R1 && x.R2 == y.R2)
            {
                return 0;
            }

            float xCX1 = x.C1.X;
            float xCX2 = x.C2.X;
            float xCY1 = x.C1.Y;
            float xCY2 = x.C2.Y;

            float yCX1 = y.C1.X;
            float yCX2 = y.C2.X;
            float yCY1 = y.C1.Y;
            float yCY2 = y.C2.Y;

            if (Within(x.XLeft, y.XLeft, y.XRight))
            {
                // y left
                TryGetIntersection(x.XLeft, y.R1.X, y.R1.Y, y.R2.X, y.R2.Y, ref yCX1, ref yCY1);
            }

            if (Within(x.XRight, y.XLeft, y.XRight))
            {
                // y right
                TryGetIntersection(x.XRight, y.R1.X, y.R1.Y, y.R2.X, y.R2.Y, ref yCX2, ref yCY2);
            }

            if (Within(y.XLeft, x.XLeft, x.XRight))
            {
                // x left
                TryGetIntersection(y.XLeft, x.R1.X, x.R1.Y, x.R2.X, x.R2.Y, ref xCX1, ref xCY1);
            }

            if (Within(y.XRight, x.XLeft, x.XRight))
            {
                // x right
                TryGetIntersection(y.XRight, x.R1.X, x.R1.Y, x.R2.X, x.R2.Y, ref xCX2, ref xCY2);
            }

            // for connected walls, only compare the un-connected vertex
            if (x.R1 == y.R1)
            {
                return Compare(xCX2, xCY2, yCX2, yCY2);
            }
            else if (x.R2 == y.R2)
            {
                return Compare(xCX1, xCY1, yCX1, yCY1);
            }
            else if (x.R1 == y.R2)
            {
                return Compare(xCX2, xCY2, yCX1, yCY1);
            }
            else if (x.R2 == y.R1)
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
