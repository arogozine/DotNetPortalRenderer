using RenderingEngine.Models;
using System.Runtime.CompilerServices;

namespace RenderingEngine.Engine
{
    internal sealed class WallComparer : IComparer<Wall>
    {
        private WallComparer() { }

        public static readonly WallComparer Instance = new();

        public int Compare(Wall? x, Wall? y)
        {
            ArgumentNullException.ThrowIfNull(x);
            ArgumentNullException.ThrowIfNull(y);

            // Debug.WriteLine($"X {x.YLeftFloor} {x.YRightFloor}");
            // Debug.WriteLine($"Y {y.YLeftFloor} {y.YRightFloor}");


            /*
            int a = Compare2(x, y);
            int b = Compare2(y, x);
            int c = Compare2(x, x);
            int d = Compare2(y, y);

            return a;
            */

            return Compare2(x, y);
        }


        public static int Compare2(Wall x, Wall y)
        {
            int aLeftFloor = x.YLeftFloor;
            int aRightFloor = x.YRightFloor;
            int bLeftFloor = y.YLeftFloor;
            int bRightFloor = y.YRightFloor;

            if (
                (aLeftFloor == bLeftFloor && aRightFloor == bRightFloor) ||
                (aRightFloor == bLeftFloor && aLeftFloor == bRightFloor)
                )
            {
                return 0;
            }

            // check left or right point is in front
            bool leftInFront = aLeftFloor >= bLeftFloor && aLeftFloor >= bRightFloor;
            bool rightInFront = aRightFloor >= bLeftFloor && aRightFloor >= bRightFloor;

            if (leftInFront && rightInFront)
            {
                // both points of wall a are in front of b
                return -1;
            }

            if (!leftInFront && !rightInFront)
            {
                // both points of wall b are in front a
                return 1;
            }

            // calculate distance from floor for the x values where walls interesect
            if (Within(x.XLeft, y.XLeft, y.XRight))
            {
                float floorDistIncr = (bRightFloor - (float)bLeftFloor) / (y.XRight - y.XLeft);
                bLeftFloor += (int)((x.XLeft - y.XLeft) * floorDistIncr);
            }

            if (Within(x.XRight, y.XLeft, y.XRight))
            {
                float floorDistIncr = (bRightFloor - (float)bLeftFloor) / (y.XRight - y.XLeft);
                bRightFloor -= (int)((y.XRight - x.XRight) * floorDistIncr);
            }

            if (Within(y.XLeft, x.XLeft, x.XRight))
            {
                float floorDistIncr = (aRightFloor - (float)aLeftFloor) / (x.XRight - x.XLeft);
                aLeftFloor += (int)((y.XLeft - x.XLeft) * floorDistIncr);
            }

            // is it supposed to be 3rd arg - first arg
            if (Within(y.XRight, x.XLeft, x.XRight))
            {
                float floorDistIncr = (aRightFloor - (float)aLeftFloor) / (x.XRight - x.XLeft);
                aRightFloor -= (int)((x.XRight - y.XRight) * floorDistIncr);
            }

            leftInFront = aLeftFloor >= bLeftFloor && aLeftFloor >= bRightFloor;
            rightInFront = aRightFloor >= bLeftFloor && aRightFloor >= bRightFloor;

            if (leftInFront && rightInFront)
            {
                // both points of wall a are in front of b
                return -1;
            }

            if (!leftInFront && !rightInFront)
            {
                // both points of wall b are in front a
                return 1;
            }

            // heuristic that works for connected walls
            int aFloor = Math.Max(aLeftFloor, aRightFloor);
            int bFloor = Math.Max(bLeftFloor, bRightFloor);

            if (aFloor == bFloor)
            {
                aFloor = aLeftFloor + aRightFloor;
                bFloor = bLeftFloor + bRightFloor;
            }

            return aFloor == bFloor ? 0 :
                   aFloor > bFloor ? -1 : 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool Within(int value, int from, int to)
            {
                return value > from && value < to;
            }
        }
    }
}
