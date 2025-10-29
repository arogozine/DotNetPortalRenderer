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

            return Compare(
                    (xCX1 + xCX2) / 2f,
                    (xCY1 + xCY2) / 2f,
                    (yCX1 + yCX2) / 2f,
                    (yCY1 + yCY2) / 2f
                );

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
    }
}
