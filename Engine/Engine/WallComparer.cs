using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    /// <summary>
    /// Wall distance comparer
    /// </summary>
    [SkipLocalsInit]
    internal sealed class WallComparer : IComparer<Wall>
    {
        public readonly float cameraWidthIncr;

        public WallComparer(int width)
        {
            cameraWidthIncr = 2.0f / width;
        }

        public int Compare(Wall? x, Wall? y)
        {
            ArgumentNullException.ThrowIfNull(x);
            ArgumentNullException.ThrowIfNull(y);

            bool r1eqr1 = x.R1 == y.R1;
            bool r2eqr2 = x.R2 == y.R2;
            bool r1eqr2 = x.R1 == y.R2;
            bool r2eqr1 = x.R2 == y.R1;

            // Same line
            if ((r1eqr1 && r2eqr2) || (r1eqr2 && r2eqr1))
            {
                return 0;
            }

            Point xC1 = x.C1;
            Point xC2 = x.C2;
            Point yC1 = y.C1;
            Point yC2 = y.C2;

            // the two line share a point, compare the other point
            if (r1eqr1)
            {
                return Compare(xC2.X, xC2.Y, yC2.X, yC2.Y);
            }
            else if (r2eqr2)
            {
                return Compare(xC1.X, xC1.Y, yC1.X, yC1.Y);
            }
            else if (r1eqr2)
            {
                return Compare(xC2.X, xC2.Y, yC1.X, yC1.Y);
            }
            else if (r2eqr1)
            {
                return Compare(xC1.X, xC1.Y, yC2.X, yC2.Y);
            }

            float xCX1 = xC1.X;
            float xCX2 = xC2.X;
            float xCY1 = xC1.Y;
            float xCY2 = xC2.Y;
            float yCX1 = yC1.X;
            float yCX2 = yC2.X;
            float yCY1 = yC1.Y;
            float yCY2 = yC2.Y;

            if (Within(x.XLeft, y.XLeft, y.XRight) || Within(x.XRight, y.XLeft, y.XRight))
            {
                CalculatePlaneIntersectionsForWall(y.XLeft, y.XRight, ref xCX1, ref xCY1, ref xCX2, ref xCY2);
            }

            if (Within(y.XLeft, x.XLeft, x.XRight) || Within(y.XRight, x.XLeft, x.XRight))
            {
                CalculatePlaneIntersectionsForWall(x.XLeft, x.XRight, ref yCX1, ref yCY1, ref yCX2, ref yCY2);
            }

            float xd1 = xCX1 * xCX1 + xCY1 * xCY1;
            float xd2 = xCX2 * xCX2 + xCY2 * xCY2;

            float yd1 = yCX1 * yCX1 + yCY1 * yCY1;
            float yd2 = yCX2 * yCX2 + yCY2 * yCY2;

            float xd = MathF.Max(xd1, xd2);
            float yd = MathF.Max(yd1, yd2);

            int compare = xd < yd ? -1 : 1;

            return compare;

            // midpoint actuall needs div by 2, but we can skip it here (values will be 4 times bigger that's all)
            return Compare(
                xCX1 + xCX2,
                xCY1 + xCY2,
                yCX1 + yCX2,
                yCY1 + yCY2
            );
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Compare(float xcx, float xcy, float ycx, float ycy)
        {
            float xd = xcx * xcx + xcy * xcy;
            float yd = ycx * ycx + ycy * ycy;

            if (xd == yd)
            {
                return 0;
            }

            return xd < yd ? -1 : 1;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Within(int value, int from, int to)
        {
            return value > from && value < to;
        }

        private bool CalculatePlaneIntersectionsForWall(float xLeft, float xRight, ref float rx1, ref float ry1, ref float rx2, ref float ry2)
        {
            float d2x = rx2 - rx1;
            float d2y = ry2 - ry1;

            float rayDirLeft = EngineConstants.CameraPlaneX * (cameraWidthIncr * xLeft - 1f);
            float rayDirRight = EngineConstants.CameraPlaneX * (cameraWidthIncr * xRight - 1f);

            bool intersectsL = TryGetSegmentIntersectionZero2(rayDirLeft, rx1, ry1, d2x, d2y,
                out float xDistanceL, out float yDistanceL);

            bool intersectsR = TryGetSegmentIntersectionZero2(rayDirRight, rx2, ry2, -d2x, -d2y,
                out float xDistanceR, out float yDistanceR);

            if (intersectsL && intersectsR)
            {
                rx1 = xDistanceL;
                ry1 = yDistanceL;

                rx2 = xDistanceR;
                ry2 = yDistanceR;

                return true;
            }
            else if (intersectsL)
            {
                rx1 = xDistanceL;
                ry1 = yDistanceL;
            }
            else if (intersectsR)
            {
                rx2 = xDistanceR;
                ry2 = yDistanceR;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryGetSegmentIntersectionZero2(
            float rayDirX,
            float rx1, float ry1,
            float d2x, float d2y,
            out float distanceX,
            out float distanceY)
        {
            Unsafe.SkipInit(out distanceX);
            Unsafe.SkipInit(out distanceY);

            float denominator = rayDirX * d2y - d2x;

            if (MathF.Abs(denominator) < float.Epsilon)
            {
                return false;
            }

            float u = (rx1 - ry1 * rayDirX) / denominator;

            if (u < 0f || u > 1f)
            {
                return false;
            }

            float t = (rx1 * d2y - ry1 * d2x) / denominator;

            if (t < 0f)
            {
                return false;
            }

            distanceY = t;
            distanceX = t * rayDirX;

            return true;
        }

    }
}
