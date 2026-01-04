using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    internal sealed class SpriteComparer : IComparer<Sprite>
    {
        public int Compare(Sprite? x, Sprite? y)
        {
            ArgumentNullException.ThrowIfNull(x);
            ArgumentNullException.ThrowIfNull(y);

            if (y.Rotated == x.Rotated)
            {
                return 0;
            }

            float xcx = x.Rotated.X;
            float xcy = x.Rotated.Y;
            float ycx = y.Rotated.X;
            float ycy = y.Rotated.Y;

            float xd = xcx * xcx + xcy * xcy;
            float yd = ycx * ycx + ycy * ycy;

            if (xd == yd)
            {
                return 0;
            }

            return xd < yd ? 1 : -1;
        }
    }
}
