using SoftwareRendererModels;
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

            return (x.Bunch ?? 0) - (y.Bunch ?? 0);
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
            Debug.Assert(x != null);
            Debug.Assert(y != null);

            // AI Assisted
            bool r1eqr1 = x.R1 == y.R1;
            bool r2eqr2 = x.R2 == y.R2;
            bool r1eqr2 = x.R1 == y.R2;
            bool r2eqr1 = x.R2 == y.R1;

            // Same line
            if ((r1eqr1 && r2eqr2) || (r1eqr2 && r2eqr1))
            {
                return 0;
            }

            float xCY1 = x.C1.Y;
            float xCY2 = x.C2.Y;
            float yCY1 = y.C1.Y;
            float yCY2 = y.C2.Y;

            // the two lines share a point; compare the other point
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

            bool intersects = false;

            // if the walls overlap, find the the distance at the overlapping point
            if (Within(x.XLeft, y.XLeft, y.XRight) || Within(x.XRight, y.XLeft, y.XRight))
            {
                float yRX1 = y.C1.X;
                float yRX2 = y.C2.X;
                float yRY1 = y.C1.Y;
                float yRY2 = y.C2.Y;

                (bool left, bool right) = CalculatePlaneIntersectionsForWall(x.XLeft, x.XRight, ref yRX1, ref yRY1, ref yRX2, ref yRY2);

                if (left)
                {
                    yCY1 = yRY1;
                }

                if (right)
                {
                    yCY2 = yRY2;
                }

                intersects |= left || right;
            }

            if (Within(y.XLeft, x.XLeft, x.XRight) || Within(y.XRight, x.XLeft, x.XRight))
            {
                float xRX1 = x.C1.X;
                float xRX2 = x.C2.X;
                float xRY1 = x.C1.Y;
                float xRY2 = x.C2.Y;

                (bool left, bool right) = CalculatePlaneIntersectionsForWall(y.XLeft, y.XRight, ref xRX1, ref xRY1, ref xRX2, ref xRY2);

                if (left)
                {
                    xCY1 = xRY1;
                }

                if (right)
                {
                    xCY2 = xRY2;
                }

                intersects |= left || right;
            }

            if (intersects)
            {
                float xd = (xCY1 + xCY2) / 2f;
                float yd = (yCY1 + yCY2) / 2f;

                return Compare(xd, yd);
            }

            return Compare(x.AvgDepth, y.AvgDepth);

            /*
            // clip both walls to the overlap region so each is measured at identical screen columns
            int overlapLeft  = Math.Max(x.XLeft, y.XLeft);
            int overlapRight = Math.Min(x.XRight, y.XRight);

            if (overlapLeft > overlapRight)
            {
                return Compare(x.AvgDepth, y.AvgDepth);
            }

            // AI Assisted: use C1/C2 (clipped, always Y>0) instead of R1/R2 which can be behind the camera
            float yCx1 = y.C1.X, yCy1 = y.C1.Y;
            float yCx2 = y.C2.X, yCy2 = y.C2.Y;
            (bool yL, bool yR) = CalculatePlaneIntersectionsForWall(overlapLeft, overlapRight, ref yCx1, ref yCy1, ref yCx2, ref yCy2);
            if (yL) yCY1 = yCy1;
            if (yR) yCY2 = yCy2;

            float xCx1 = x.C1.X, xCy1 = x.C1.Y;
            float xCx2 = x.C2.X, xCy2 = x.C2.Y;
            (bool xL, bool xR) = CalculatePlaneIntersectionsForWall(overlapLeft, overlapRight, ref xCx1, ref xCy1, ref xCx2, ref xCy2);
            if (xL) xCY1 = xCy1;
            if (xR) xCY2 = xCy2;

            return Compare((xCY1 + xCY2) * 0.5f, (yCY1 + yCY2) * 0.5f);
            */
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

            bool intersectsL = MathFormulas.TryGetSegmentIntersectionZero2(rayDirLeft, rx1, ry1, d2x, d2y,
                out float xDistanceL, out float yDistanceL);

            bool intersectsR = MathFormulas.TryGetSegmentIntersectionZero2(rayDirRight, rx2, ry2, -d2x, -d2y,
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
    }
}
