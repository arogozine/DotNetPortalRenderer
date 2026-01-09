using RenderingEngine.Models;
using static RenderingEngine.Engine.SharedHelpers;

namespace RenderingEngine.Engine
{
    internal sealed class BunchComparer : IComparer<RenderableWall>
    {
        private BunchComparer() { }

        public static readonly BunchComparer Default = new();

        public int Compare(RenderableWall? x, RenderableWall? y)
        {
            ArgumentNullException.ThrowIfNull(x);
            ArgumentNullException.ThrowIfNull(y);

            return x.Bunch - y.Bunch;
        }
    }

    /// <summary>
    /// Wall distance comparer
    /// </summary>
    [SkipLocalsInit]
    internal sealed class WallComparer : IComparer<RenderableWall>
    {
        public readonly float cameraWidthIncr;

        public WallComparer(int width)
        {
            cameraWidthIncr = 2.0f / width;
        }

        public int Compare(RenderableWall? x, RenderableWall? y)
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

            float xCY1 = xC1.Y;
            float xCY2 = xC2.Y;
            float yCY1 = yC1.Y;
            float yCY2 = yC2.Y;

            // the two line share a point, compare the other point
            if (r1eqr1)
            {
                return Compare(xCY2, yCY2);
            }
            else if (r2eqr2)
            {
                return Compare(xCY1, yCY1);
            }
            else if (r1eqr2)
            {
                return Compare(xCY2, yCY1);
            }
            else if (r2eqr1)
            {
                return Compare(xCY1, yCY2);
            }

            // if the walls overlap, find the the distance at the overlapping point
            if (Within(x.XLeft, y.XLeft, y.XRight) || Within(x.XRight, y.XLeft, y.XRight))
            {
                float yRX1 = y.R1.X;
                float yRX2 = y.R2.X;
                float yRY1 = y.R1.Y;
                float yRY2 = y.R2.Y;

                (bool left, bool right) = CalculatePlaneIntersectionsForWall(x.XLeft, x.XRight, ref yRX1, ref yRY1, ref yRX2, ref yRY2);

                if (left)
                {
                    yCY1 = yRY1;
                }

                if (right)
                {
                    yCY2 = yRY2;
                }

                if (left && right)
                {
                    return CompareFurtherst(xCY1, xCY2, yCY1, yCY2);
                }
                else if (left)
                {
                    return Compare(xCY1, yCY1);
                }
                else if (right)
                {
                    return Compare(xCY2, yCY2);
                }
            }

            if (Within(y.XLeft, x.XLeft, x.XRight) || Within(y.XRight, x.XLeft, x.XRight))
            {
                float xRX1 = x.R1.X;
                float xRX2 = x.R2.X;
                float xRY1 = x.R1.Y;
                float xRY2 = x.R2.Y;

                (bool left, bool right) = CalculatePlaneIntersectionsForWall(y.XLeft, y.XRight, ref xRX1, ref xRY1, ref xRX2, ref xRY2);

                if (left)
                {
                    xCY1 = xRY1;
                }

                if (right)
                {
                    xCY2 = xRY2;
                }

                if (left && right)
                {
                    return CompareFurtherst(xCY1, xCY2, yCY1, yCY2);
                }
                else if (left)
                {
                    return Compare(xCY1, yCY1);

                }
                else if (right)
                {
                    return Compare(xCY2, yCY2);
                }
            }

            return CompareFurtherst(xCY1, xCY2, yCY1, yCY2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CompareFurtherst(float xCY1, float xCY2, float yCY1, float yCY2)
        {
            float xd = MathF.Max(xCY1, xCY2);
            float yd = MathF.Max(yCY1, yCY2);

            return Compare(xd, yd);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Compare(float xcy, float ycy)
        {
            if (xcy == ycy)
            {
                return 0;
            }

            return xcy < ycy ? -1 : 1;
        }

        private (bool left, bool right) CalculatePlaneIntersectionsForWall(float xLeft, float xRight, ref float rx1, ref float ry1, ref float rx2, ref float ry2)
        {
            float d2x = rx2 - rx1;
            float d2y = ry2 - ry1;

            float rayDirLeft = MathF.FusedMultiplyAdd(cameraWidthIncr, xLeft, -1f);
            float rayDirRight = MathF.FusedMultiplyAdd(cameraWidthIncr, xRight, -1f);

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

            return (intersectsL, intersectsR);
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
