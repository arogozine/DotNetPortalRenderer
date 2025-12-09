using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    /// <summary>
    /// Wall distance comparer
    /// </summary>
    [SkipLocalsInit]
    internal sealed class WallComparer : IComparer<Wall>
    {
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

           /*
            // the two lines intersect, compare intersection
            bool intersectsX1 = TryGetSegmentIntersectionFromZero(xC1, y.R1, y.R2, out Point intersection1);
            bool intersectsX2 = TryGetSegmentIntersectionFromZero(xC2, y.R1, y.R2, out Point intersection2);
            bool intersectsY1 = TryGetSegmentIntersectionFromZero(yC1, x.R1, x.R2, out Point intersection3);
            bool intersectsY2 = TryGetSegmentIntersectionFromZero(yC2, x.R1, x.R2, out Point intersection4);

            if (intersectsX1)
            {
                yC1 = intersection1;
            }

            if (intersectsX2)
            {
                yC2 = intersection2;
            }

            if (intersectsY1 && !intersectsX1)
            {
                xC1 = intersection3;
            }

            if (intersectsY2 && !intersectsX2)
            {
                xC2 = intersection4;
            }
            */


            float xCX1 = xC1.X;
            float xCX2 = xC2.X;
            float xCY1 = xC1.Y;
            float xCY2 = xC2.Y;
            float yCX1 = yC1.X;
            float yCX2 = yC2.X;
            float yCY1 = yC1.Y;
            float yCY2 = yC2.Y;

            float xd1 = xCX1 * xCX1 + xCY1 * xCY1;
            float xd2 = xCX2 * xCX2 + xCY2 * xCY2;

            float yd1 = yCX1 * yCX1 + yCY1 * yCY1;
            float yd2 = yCX2 * yCX2 + yCY2 * yCY2;

            float xd = MathF.Max(xd1, xd2);
            float yd = MathF.Max(yd1, yd2);

            int compare = xd < yd ? -1 : 1;

            return compare;

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

        public static bool TryGetSegmentIntersectionFromZero(
            Point p2,
            Point p3,
            Point p4,
            out Point intersection)
        {
            intersection = default;

            float d1x = p2.X;
            float d1y = p2.Y;
            float d2x = p4.X - p3.X;
            float d2y = p4.Y - p3.Y;

            float denominator = d1x * d2y - d1y * d2x;

            if (MathF.Abs(denominator) < float.Epsilon)
            {
                return false;
            }

            float d3x = p3.X;
            float d3y = p3.Y;

            float u = (d3x * d1y - d3y * d1x) / denominator;

            if (u < 0 || u > 1)
                return false;

            float t = (d3x * d2y - d3y * d2x) / denominator;

            if (t < 0)
                return false;

            intersection = new Point(
                t * d1x,
                t * d1y
            );

            return true;
        }

        public static bool TryGetSegmentIntersection(
    Point p1,
    Point p2,
    Point p3,
    Point p4,
    out Point intersection)
        {
            intersection = default;

            float d1x = p2.X - p1.X;
            float d1y = p2.Y - p1.Y;
            float d2x = p4.X - p3.X;
            float d2y = p4.Y - p3.Y;

            float denominator = d1x * d2y - d1y * d2x;

            if (MathF.Abs(denominator) < float.Epsilon)
            {
                return false;
            }

            float d3x = p3.X - p1.X;
            float d3y = p3.Y - p1.Y;

            float u = (d3x * d1y - d3y * d1x) / denominator;

            if (u < 0 || u > 1)
                return false;

            float t = (d3x * d2y - d3y * d2x) / denominator;

            if (t < 0)
                return false;

            intersection = new Point(
                t * d1x + p1.X,
                t * d1y + p1.Y
            );
            //Vector128.FusedMultiplyAdd(Vector128.Create(t), d1, p1);

            return true;
        }
    }
}
