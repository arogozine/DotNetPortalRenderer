using RenderingEngine.Models;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace RenderingEngine.Engine
{
    internal sealed class WallHelper
    {
        private readonly int width;
        private readonly int height;
        private readonly float cameraPlaneX;
        private readonly float vFov;

        public WallHelper(
            int width,
            int height,
            float cameraPlaneX, float vFov)
        {
            this.width = width;
            this.height = height;
            this.cameraPlaneX = cameraPlaneX;
            this.vFov = vFov;
        }

        public Span<Wall> DetermineWallsToRender(Sector sector,
            List<Wall> portalWallsToOcclude,
            float pSin, float pCos, float px, float py, float yCeil, float yFloor, float yaw)
        {
            Span<Wall> rotatedWalls = RotateSectorWallsRelativeToPlayer(sector, pSin, pCos, px, py);

            Span<Range> bunches = BreakUpIntoBunches(rotatedWalls);

            FilterOutWallsBehindPlayer(bunches, rotatedWalls);

            CalculateWallPlanes(bunches, rotatedWalls, yCeil, yFloor, yaw);

            FilterOutWallsOutsideView(bunches, rotatedWalls);

            Span<Wall> result = CullHiddenWallsAndCombineBunches(bunches, rotatedWalls, portalWallsToOcclude);

            result.Sort(WallComparer.Instance);

            return result;
        }

        private static void FilterParentPortalWall(ref Span<Wall> rotatedWalls, Wall? parentSectorWall)
        {
            if (parentSectorWall is null)
            {
                return;
            }

            for (int i = 0; i < rotatedWalls.Length; i++)
            {
                Wall wall = rotatedWalls[i];

                if (SameLine(wall, parentSectorWall))
                {
                    for (int j = i + 1; j < rotatedWalls.Length; j++, i++)
                    {
                        rotatedWalls[i] = rotatedWalls[j];
                    }

                    rotatedWalls = rotatedWalls[0..i];
                    return;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool SameLine(Wall a, Wall b)
        {
            if (a is not null && b is not null)
            {
                return (
                    a.X1 == b.X1
                    && a.X2 == b.X2
                    && a.Y1 == b.Y1
                    && a.Y2 == b.Y2
                    ) ||
                    (
                    a.X2 == b.X1
                    && a.X1 == b.X2
                    && a.Y2 == b.Y1
                    && a.Y1 == b.Y2
                );
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static WallYPlaneInfo CalculateLeftWallYPlaneInfo(Wall wall, int wallFromXOffset)
        {
            float wallStartY = wall.YLeftCeil;
            float ceilDistIncr = (wall.YRightCeil - (float)wall.YLeftCeil) / (wall.XRight - wall.XLeft);

            float wallEndY = wall.YLeftFloor;
            float floorDistIncr = (wall.YRightFloor - (float)wall.YLeftFloor) / (wall.XRight - wall.XLeft);

            if (wallFromXOffset != 0)
            {
                wallEndY += wallFromXOffset * floorDistIncr;
                wallStartY += wallFromXOffset * ceilDistIncr;
            }

            return new WallYPlaneInfo(wallStartY, wallEndY, ceilDistIncr, floorDistIncr);
        }

        public static Span<Wall> RotateSectorWallsRelativeToPlayer(Sector sector, float pSin, float pCos, float px, float py)
        {
            ReadOnlySpan<Wall> walls = sector.Walls;
            Span<Wall> rotatedWalls = new Wall[walls.Length];

            // Rotate relative to player
            for (int i = 0; i < walls.Length; i++)
            {
                rotatedWalls[i] = RotateWall(walls[i], pSin, pCos, px, py);
            }

            return rotatedWalls;
        }

        public static Span<Range> BreakUpIntoBunches(Span<Wall> rotatedWalls)
        {
            // a bunch is a set of connected walls
            // we figure out the range of each bunch here

            Span<Range> bunches = new Range[rotatedWalls.Length];

            int bunchCount = 0;
            int subsetStart = 0;

            if (rotatedWalls.Length <= 1)
            {
                bunches[0] = 0..rotatedWalls.Length;
                return bunches[..rotatedWalls.Length];
            }

            for (int i = 0, b = 1; b < rotatedWalls.Length; i++, b++)
            {
                Wall current = rotatedWalls[i];
                Wall next = rotatedWalls[b];

                bool leftConnects = current.X1 == next.X1 && current.Y1 == next.Y1 ||
                    current.X1 == next.X2 && current.Y1 == next.Y2;

                bool rightConnects = current.X2 == next.X1 && current.Y2 == next.Y1 ||
                    current.X2 == next.X2 && current.Y2 == next.Y2;

                // New Bunch = Not Connected to Previous Wall
                if (!leftConnects && !rightConnects)
                {
                    bunches[bunchCount] = subsetStart..b;
                    bunchCount++;
                    subsetStart = b;
                }
                else if (b + 1 == rotatedWalls.Length)
                {
                    bunches[bunchCount] = subsetStart..rotatedWalls.Length;
                    bunchCount++;
                }
            }

            return bunches[..bunchCount];
        }

        public static void FilterOutWallsBehindPlayer(ref Span<Wall> walls)
        {
            // in-place sort out walls and trim the span

            int j = 0;

            for (int i = 0; i < walls.Length; i++)
            {
                Wall wall = walls[i];

                if (wall.Y1 <= 0f && wall.Y2 <= 0f)
                {
                    continue;
                }

                walls[j] = wall;
                j++;
            }

            walls = walls[..j];
        }

        public static void FilterOutWallsBehindPlayer(Span<Range> bunches, Span<Wall> rotatedWalls)
        {
            for (int s = 0; s < bunches.Length; s++)
            {
                Range range = bunches[s];
                Span<Wall> bunch = rotatedWalls[range];
                FilterOutWallsBehindPlayer(ref bunch);
                bunches[s] = new Range(range.Start, new Index(bunch.Length + range.Start.Value));
            }
        }

        public void CalculateWallPlanes(Span<Range> bunches, Span<Wall> rotatedWalls, float yCeil, float yFloor, float yaw)
        {
            for (int b = 0; b < bunches.Length; b++)
            {
                CalculateWallPlanes(rotatedWalls[bunches[b]], yCeil, yFloor, yaw);
            }
        }

        public void CalculateWallPlanes(Span<Wall> walls, float yCeil, float yFloor, float yaw)
        {
            for (int i = 0; i < walls.Length; i++)
            {
                Wall wall = walls[i];

                CalculateWallPlane(wall, yCeil, yFloor, yaw);
            }
        }

        public static void FilterOutWallsOutsideView(Span<Range> bunches, Span<Wall> rotatedWalls)
        {
            for (int s = 0; s < bunches.Length; s++)
            {
                Range range = bunches[s];
                Span<Wall> bunch = rotatedWalls[range];
                FilterOutWallsOutsideView(ref bunch);
                bunches[s] = new Range(range.Start, new Index(bunch.Length + range.Start.Value));
            }

            static void FilterOutWallsOutsideView(ref Span<Wall> walls)
            {
                // in-place sort out walls and trim the span

                int j = 0;

                for (int i = 0; i < walls.Length; i++)
                {
                    Wall wall = walls[i];

                    if (!wall.IntersectsView)
                    {
                        continue;
                    }

                    if (wall.CY1 <= 0f || wall.CY2 <= 0f)
                    {
                        continue;
                    }

                    walls[j] = wall;
                    j++;
                }

                walls = walls[..j];
            }
        }
        private static Span<Point> GetUniquePoints(Span<Wall> bunch)
        {
            int n = bunch.Length << 1;
            Span<Point> points = new Point[n];

            for (int i = 0, j = 0; i < bunch.Length; i++)
            {
                Wall wall = bunch[i];
                points[j++] = new Point(wall.X1, wall.Y1);
                points[j++] = new Point(wall.X2, wall.Y2);
            }

            return DistinctInPlace(points);
        }

        private static Span<T> DistinctInPlace<T>(Span<T> span)
        {
            if (span.Length <= 1)
            {
                return span;
            }

            var seen = new HashSet<T>();
            int writeIndex = 0;

            for (int readIndex = 0; readIndex < span.Length; readIndex++)
            {
                T current = span[readIndex];

                if (seen.Add(current))
                {
                    span[writeIndex] = current;
                    writeIndex++;
                }
            }

            return span[..writeIndex];
        }

        private static bool IsPointInPolygon(Span<Point> vertexes, float x, float y)
        {
            int n = vertexes.Length;
            bool isInside = false;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Point vertexI = vertexes[i];
                Point vertexJ = vertexes[j];

                if (vertexI.Y > y != vertexJ.Y > y &&
                    x < (vertexJ.X - vertexI.X) * (y - vertexI.Y) / (vertexJ.Y - vertexI.Y) + vertexI.X)
                {
                    isInside = !isInside;
                }
            }

            return isInside;
        }

        public Span<Wall> CullHiddenWallsAndCombineBunches(
            Span<Range> bunches, Span<Wall> rotatedWalls, List<Wall> parentPortalWallsToOcclude)
        {
            Span<Wall> finalWalls = new Wall[rotatedWalls.Length];

            int i = 0;
            for (int s = 0; s < bunches.Length; s++)
            {
                Span<Wall> visible = rotatedWalls[bunches[s]];

                CullWallsFromBunch(ref visible, parentPortalWallsToOcclude);

                for (int v = 0; v < visible.Length; v++)
                {
                    finalWalls[i++] = visible[v];
                }
            }

            return finalWalls[..i];
        }

        public void CullWallsFromBunch(ref Span<Wall> walls, List<Wall> parentPortalWallsToOcclude)
        {
            foreach (Wall parentSectorWall in parentPortalWallsToOcclude)
            {
                FilterParentPortalWall(ref walls, parentSectorWall);
            }

            if (walls.IsEmpty)
            {
                return;
            }

            bool[] visibility = new bool[this.width];
            visibility.AsSpan().Fill(true);

            int j = 0;
            for (int i = 0; i < walls.Length; i++)
            {
                Wall wall = walls[i];

                bool hidden = true;
                for (int v = wall.XLeft; v <= wall.XRight; v++)
                {
                    hidden &= !visibility[v];
                    visibility[v] = true;
                }

                if (hidden)
                {
                    continue;
                }

                walls[j] = wall;
                j++;
            }

            walls = walls[..j];

            return;
        }

        public void CalculateWallPlane(Wall wall, float yCeil, float yFloor, float yaw)
        {
            // calculate the x, y for the wall on the screen for both points
            float rx1 = wall.X1;
            float ry1 = wall.Y1;
            float rx2 = wall.X2;
            float ry2 = wall.Y2;

            float xLeft, xRight, yLeftCeil, yLeftFloor, yRightCeil, yRightFloor;
            float scale = width * -0.7575231f;
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;

            xLeft = halfWidth - rx1 / ry1 * scale;
            xRight = halfWidth - rx2 / ry2 * scale;

            // order left to right
            if (xLeft > xRight)
            {
                (xLeft, xRight) = (xRight, xLeft);

                (rx1, rx2) = (rx2, rx1);
                (ry1, ry2) = (ry2, ry1);

                (wall.X1, wall.X2) = (wall.X2, wall.X1);
                (wall.Y1, wall.Y2) = (wall.Y2, wall.Y1);
            }

            // part of the wall is in the back
            if (ry1 <= 0f || ry2 <= 0f)
            {
                float d2x = rx2 - rx1;
                float d2y = ry2 - ry1;

                bool intersectsL = TryGetSegmentIntersectionZero2(-cameraPlaneX, rx1, ry1, d2x, d2y,
                    out float xDistanceL, out float yDistanceL);

                bool intersectsR = TryGetSegmentIntersectionZero2(cameraPlaneX, rx1, ry1, d2x, d2y,
                    out float xDistanceR, out float yDistanceR);

                // Clamp(ref xLeft, ref xRight);

                if (intersectsL && intersectsR)
                {
                    rx1 = xDistanceL;
                    ry1 = yDistanceL;

                    rx2 = xDistanceR;
                    ry2 = yDistanceR;

                    xLeft = 0;
                    xRight = width - 1;

                    wall.IntersectsView = true;
                }
                else if (intersectsL || intersectsR)
                {
                    float xDistance = intersectsL ? xDistanceL : xDistanceR;
                    float yDistance = intersectsL ? yDistanceL : yDistanceR;

                    if (ry1 <= 0f)
                    {
                        rx1 = xDistance;
                        ry1 = yDistance;
                        xLeft = halfWidth - rx1 / ry1 * scale;
                    }
                    else
                    {
                        rx2 = xDistance;
                        ry2 = yDistance;
                        xRight = halfWidth - rx2 / ry2 * scale;
                    }
                }
                else
                {
                    // despite one the wall going behind the player's view
                    // player's view doesn't intersect at corners
                    // we assume wall can't be drawn
                    wall.IntersectsView = false;
                    return;
                }
            }

            if (xLeft == xRight)
            {
                wall.IntersectsView = false;
                return;
            }

            Clamp(ref xLeft, ref xRight);

            // order left to right
            if (xLeft > xRight)
            {
                (xLeft, xRight) = (xRight, xLeft);

                (rx1, rx2) = (rx2, rx1);
                (ry1, ry2) = (ry2, ry1);

                (wall.X1, wall.X2) = (wall.X2, wall.X1);
                (wall.Y1, wall.Y2) = (wall.Y2, wall.Y1);
            }

            wall.IntersectsView |= CalculatePlaneIntersectionsForWall(ref xLeft, ref xRight, ref rx1, ref ry1, ref rx2, ref ry2);

            if (wall.IntersectsView)
            {
                yLeftCeil = halfHeight - (yCeil / ry1 - yaw) * vFov;
                yLeftFloor = halfHeight - (yFloor / ry1 - yaw) * vFov;
                yRightCeil = halfHeight - (yCeil / ry2 - yaw) * vFov;
                yRightFloor = halfHeight - (yFloor / ry2 - yaw) * vFov;

                wall.CX1 = rx1;
                wall.CY1 = ry1;
                wall.CX2 = rx2;
                wall.CY2 = ry2;

                wall.XLeft = (int)xLeft;
                wall.XRight = (int)xRight;
                wall.YLeftCeil = (int)yLeftCeil;
                wall.YLeftFloor = (int)yLeftFloor;
                wall.YRightCeil = (int)yRightCeil;
                wall.YRightFloor = (int)yRightFloor;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Clamp(ref float xLeft, ref float xRight)
            {
                xLeft = Math.Clamp(xLeft, 0f, width - 1f);
                xRight = Math.Clamp(xRight, 0f, width - 1f);
            }
        }

        private bool CalculatePlaneIntersectionsForWall(ref float xLeft, ref float xRight, ref float rx1, ref float ry1, ref float rx2, ref float ry2)
        {
            int xLeftInt = (int)xLeft;
            int xRightInt = (int)xRight;

            // Nothing To Render
            if (xLeftInt == xRightInt)
            {
                return false;
            }

            float cameraPlaneX = this.cameraPlaneX;

            float cameraWidthIncr = 2.0f / width;

            float d2x = rx2 - rx1;
            float d2y = ry2 - ry1;

            float rayDirLeft = cameraPlaneX * (cameraWidthIncr * xLeftInt - 1f);
            float rayDirRight = cameraPlaneX * (cameraWidthIncr * xRightInt - 1f);

            bool intersectsL = TryGetSegmentIntersectionZero2(rayDirLeft, rx1, ry1, d2x, d2y,
                out float xDistanceL, out float yDistanceL);

            bool intersectsR = TryGetSegmentIntersectionZero2(rayDirRight, rx1, ry1, d2x, d2y,
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

            xLeft = xLeftInt;
            xRight = xRightInt;

            return true;

            // Debugging Code Below

            float cameraX = -1f;

            if (!intersectsL)
            {
                cameraX += (cameraWidthIncr * (xLeftInt - 1));

                for (int x = xLeftInt - 1; x <= xRightInt; x++, cameraX += cameraWidthIncr)
                {
                    float rayDirX = cameraPlaneX * cameraX;

                    bool intersects = TryGetSegmentIntersectionZero2(rayDirX, rx1, ry1, d2x, d2y,
                        out float xDistance, out float yDistance);

                    if (intersects)
                    {
                        if (x != xLeftInt)
                            Debug.WriteLine($"intersectsL {Math.Abs(x - xLeftInt)}");

                        rx1 = xDistance;
                        ry1 = yDistance;
                        xLeftInt = x;
                        intersectsL = true;

                        break;
                    }
                }

                if (!intersectsL)
                {
                    return false;
                }
            }

            if (!intersectsR)
            {
                cameraX = -1f;
                cameraX += (cameraWidthIncr * xRightInt);

                for (int x = xRightInt; x >= xLeftInt; x--, cameraX -= cameraWidthIncr)
                {
                    float rayDirX = cameraPlaneX * cameraX;

                    bool intersects = TryGetSegmentIntersectionZero2(rayDirX, rx1, ry1, d2x, d2y,
                        out float xDistance, out float yDistance);

                    if (intersects)
                    {
                        if (x != xRightInt)
                            Debug.WriteLine($"intersectsR {Math.Abs(x - xRightInt)}");

                        rx2 = xDistance;
                        ry2 = yDistance;
                        xRightInt = x;
                        intersectsR = true;
                        break;
                    }
                }

                if (!intersectsR)
                {
                    return false;
                }
            }

            xLeft = xLeftInt;
            xRight = xRightInt;

            return intersectsL || intersectsR;

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryGetSegmentIntersectionZero2(
            float rayDirX,
            float rx1, float ry1,
            float d2x, float d2y,
            out float distanceX,
            out float distanceY)
        {
            distanceY = default;
            distanceX = default;

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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Wall RotateWall(Wall wall, float psin, float pcos, float px, float py)
        {
            // Vertex Points (Wall)
            // point 1 (vx1, vy1), point 2 (vx2, vy2)
            float vx1 = wall.X1;
            float vy1 = wall.Y1;
            float vx2 = wall.X2;
            float vy2 = wall.Y2;

            // offset by player coordinates for easier calculations
            float tx1 = vx1 - px;
            float ty1 = vy1 - py;
            float tx2 = vx2 - px;
            float ty2 = vy2 - py;

            // rotate vertex points to face 'up' from player at (0, 0)
            float rx1 = tx1 * psin - ty1 * pcos;
            float ry1 = tx1 * pcos + ty1 * psin;
            float rx2 = tx2 * psin - ty2 * pcos;
            float ry2 = tx2 * pcos + ty2 * psin;

            return new Wall(rx1, ry1, rx2, ry2, wall.Neighbor);
        }
    }
}
