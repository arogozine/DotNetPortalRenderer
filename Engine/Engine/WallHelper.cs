using RenderingEngine.Models;
using static RenderingEngine.Engine.SharedHelpers;

namespace RenderingEngine.Engine
{
    internal sealed class WallHelper
    {
        private readonly int width;
        private readonly int height;
        private readonly bool[] visibility;
        private readonly WallComparer wallComparer;
        private PortalPlayerSnapshot? _player;
        private readonly Dictionary<int, RenderableWall[]> wallCache = [];

        public WallHelper(
            int width,
            int height)
        {
            this.width = width;
            this.height = height;
            this.visibility = new bool[width];
            wallComparer = new WallComparer(width);
        }

        [MemberNotNull(nameof(_player))]
        public void SetSnapShot(PortalPlayerSnapshot player)
        {
            if (_player is PortalPlayerSnapshot old && (old.Angle != player.Angle || old.X != player.X || old.Y != player.Y || old.Z != player.Z))
            {
                wallCache.Clear();
            }

            _player = player;
        }

        public Span<RenderableWall> DetermineWallsToRender(Sector sector, Span<RenderableWall> portalWallsToOcclude, NeighborsToRender sectorInfo, PortalPlayerSnapshot player)
        {
            Span<RenderableWall> rotatedWalls = CacheRotatedWallsRelativeToPlayer(sector, player);

            rotatedWalls = CullWallsOutsideOfWindow(rotatedWalls, sectorInfo);

            Span<Range> bunches = BreakUpIntoBunches(rotatedWalls);
            Span<RenderableWall> result = CullHiddenWallsAndCombineBunches(bunches, rotatedWalls, portalWallsToOcclude);

            // we already sorted and culled bunches themselves, thus
            // if there is just one bunch, no need to sort and cull again
            if (bunches.Length > 1)
            {
                result.Sort(wallComparer);
                CullWallsBasedOnVisibility(ref result);
            }

            return result;
        }

        private static Span<RenderableWall> CullWallsOutsideOfWindow(Span<RenderableWall> rotatedWalls, NeighborsToRender sectorInfo)
        {
            RenderablePortalWall? renderableWall = sectorInfo.RenderableWall;

            if (renderableWall is null)
            {
                return rotatedWalls;
            }

            int xLeft = renderableWall.XLeft;
            int xRight = renderableWall.XRight;

            int j = 0;
            for (int i = 0; i < rotatedWalls.Length; i++)
            {
                RenderableWall wall = rotatedWalls[i];

                if (WithinInclusive(wall.XLeft, xLeft, xRight) || WithinInclusive(wall.XRight, xLeft, xRight) || WithinInclusive(xLeft, wall.XLeft, wall.XRight) || WithinInclusive(xRight, wall.XLeft, wall.XRight))
                {
                    rotatedWalls[j] = wall;
                    j++;
                }
            }

            return rotatedWalls[..j];
        }

        private void CullWallsBasedOnVisibility(ref Span<RenderableWall> walls)
        {
            Span<bool> visibility = this.visibility;
            visibility.Fill(true);

            int j = 0;
            for (int i = 0; i < walls.Length; i++)
            {
                RenderableWall wall = walls[i];
                int xLeft = wall.XLeft;
                int xRight = wall.XRight;

                Span<bool> subspan = visibility[xLeft..xRight];
                bool visible = subspan.Contains(true);
                subspan.Clear();

                if (visible)
                {
                    walls[j] = wall;
                    j++;
                }
            }

            walls = walls[..j];
        }

        private Span<RenderableWall> CacheRotatedWallsRelativeToPlayer(Sector sector, PortalPlayerSnapshot player)
        {
            RenderableWall[] copy;

            if (wallCache.TryGetValue(sector.Id, out RenderableWall[]? walls))
            {
                copy = new RenderableWall[walls.Length];
                walls.AsSpan().CopyTo(copy);
                return copy;
            }

            float pSin = player.Sin;
            float pCos = player.Cos;
            float px = player.X;
            float py = player.Y;
            float yaw = player.Yaw;
            float pz = player.Z;
            float yCeil = sector.Ceil - pz;
            float yFloor = sector.Floor - pz;

            Span<RenderableWall> rotatedWalls = RotateSectorWallsRelativeToPlayer(sector, pSin, pCos, px, py);
            FilterOutWallsBehindPlayer(ref rotatedWalls);
            CalculateWallPlanes(rotatedWalls, yCeil, yFloor, yaw);
            FilterOutWallsOutsideView(ref rotatedWalls);

            copy = new RenderableWall[rotatedWalls.Length];
            rotatedWalls.CopyTo(copy);
            wallCache[sector.Id] = copy;

            return rotatedWalls;
        }

        public static void AssignBunches(scoped ReadOnlySpan<Sector> sectors)
        {
            Queue<RenderableWall> assignedWallsQueue = [];

            for (int s = 0; s < sectors.Length; s++)
            {
                Sector sector = sectors[s];
                Span<RenderableWall> walls = sector.Walls;

                int currentGroupId = 0;

                for (int i = 0; i < walls.Length; i++)
                {
                    RenderableWall wall = walls[i];

                    // line without a bunch assigned
                    if (wall.Bunch == -1)
                    {
                        wall.Bunch = currentGroupId;
                        currentGroupId++;

                        assignedWallsQueue.Enqueue(wall);
                    }

                    // determine all connected lines
                    while (assignedWallsQueue.TryDequeue(out RenderableWall? current))
                    {
                        AssignGroup(current, walls);
                    }
                }

                // sort lines by group
                if (currentGroupId > 1)
                {
                    walls.Sort(BunchComparer.Default);
                }
            }

            return;

            void AssignGroup(RenderableWall current, scoped ReadOnlySpan<RenderableWall> walls)
            {
                for (int i = 0; i < walls.Length; i++)
                {
                    RenderableWall next = walls[i];

                    // already assigned, skip
                    if (next.Bunch != -1)
                    {
                        continue;
                    }

                    // if connects, assign to the same bunch
                    // add to queue
                    bool leftConnects = current.R1 == next.R1 || current.R1 == next.R2;
                    bool rightConnects = current.R2 == next.R1 || current.R2 == next.R2;

                    if (leftConnects || rightConnects)
                    {
                        next.Bunch = current.Bunch;
                        assignedWallsQueue.Enqueue(next);
                    }
                }
            }
        }

        private static void FilterParentPortalWall(ref Span<RenderableWall> rotatedWalls, RenderableWall parentSectorWall)
        {
            for (int i = 0; i < rotatedWalls.Length; i++)
            {
                RenderableWall wall = rotatedWalls[i];

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
        private static bool SameLine(RenderableWall a, RenderableWall b)
        {
            return (a.R1 == b.R1 && a.R2 == b.R2) ||
                (a.R2 == b.R1 && a.R1 == b.R2);
        }

        public static RenderableWall[] RotateSectorWallsRelativeToPlayer(Sector sector, float pSin, float pCos, float px, float py)
        {
            ReadOnlySpan<RenderableWall> walls = sector.Walls;
            RenderableWall[] rotatedWalls = new RenderableWall[walls.Length];

            // Rotate relative to player
            for (int i = 0; i < walls.Length; i++)
            {
                rotatedWalls[i] = RotateWall(walls[i], pSin, pCos, px, py);
            }

            return rotatedWalls;
        }

        public static Span<Range> BreakUpIntoBunches(scoped Span<RenderableWall> rotatedWalls)
        {
            // a bunch is a set of connected walls
            // we figure out the range of each bunch here

            int bunchLength = rotatedWalls.Length > 0 ? rotatedWalls.Length : 1;
            Span<Range> bunches = new Range[bunchLength];

            int bunchCount = 0;
            int subsetStart = 0;

            if (rotatedWalls.Length <= 1)
            {
                bunches[0] = 0..rotatedWalls.Length;
                return bunches[..rotatedWalls.Length];
            }

            for (int i = 0, b = 1; b < rotatedWalls.Length; i++, b++)
            {
                RenderableWall current = rotatedWalls[i];
                RenderableWall next = rotatedWalls[b];

                if (b + 1 == rotatedWalls.Length)
                {
                    bunches[bunchCount] = subsetStart..rotatedWalls.Length;
                    bunchCount++;
                }
                // New Bunch = Not Connected to Previous Wall
                else if (current.Bunch != next.Bunch)
                {
                    bunches[bunchCount] = subsetStart..b;
                    bunchCount++;
                    subsetStart = b;
                }
            }

            return bunches[..bunchCount];
        }

        public static void FilterOutWallsBehindPlayer(ref Span<RenderableWall> walls)
        {
            // in-place sort out walls and trim the span

            int j = 0;

            for (int i = 0; i < walls.Length; i++)
            {
                RenderableWall wall = walls[i];

                (float x1, float y1) = wall.R1;
                (float x2, float y2) = wall.R2;

                // wall fully behind the player
                if (y1 <= 0f && y2 <= 0f)
                {
                    continue;
                }

                if ((x1 < -y1 && x2 < -y2) || (x1 > y1 && x2 > y2))
                {
                    continue;
                }

                walls[j] = wall;
                j++;
            }

            walls = walls[..j];
        }

        public void CalculateWallPlanes(scoped Span<RenderableWall> walls, float yCeil, float yFloor, float yaw)
        {
            for (int i = 0; i < walls.Length; i++)
            {
                RenderableWall wall = walls[i];

                CalculateWallPlane(wall, yCeil, yFloor, yaw);
            }
        }

        public static void FilterOutWallsOutsideView(Span<Range> bunches, Span<RenderableWall> rotatedWalls)
        {
            for (int s = 0; s < bunches.Length; s++)
            {
                Range range = bunches[s];
                Span<RenderableWall> bunch = rotatedWalls[range];
                FilterOutWallsOutsideView(ref bunch);
                bunches[s] = new Range(range.Start, new Index(bunch.Length + range.Start.Value));
            }
        }

        private static void FilterOutWallsOutsideView(ref Span<RenderableWall> walls)
        {
            // in-place sort out walls and trim the span

            int j = 0;

            for (int i = 0; i < walls.Length; i++)
            {
                RenderableWall wall = walls[i];

                if (!wall.IntersectsView)
                {
                    continue;
                }

                if (wall.YLeftFloor < wall.YLeftCeil)
                {
                    continue;
                }

                if (wall.YLeftFloor < 0 && wall.YRightFloor < 0 && wall.YLeftCeil < 0 && wall.YRightCeil < 0)
                {
                    //continue;
                }

                walls[j] = wall;
                j++;
            }

            walls = walls[..j];
        }

        public Span<RenderableWall> CullHiddenWallsAndCombineBunches(
            scoped Span<Range> bunches, scoped Span<RenderableWall> rotatedWalls, Span<RenderableWall> parentPortalWallsToOcclude)
        {
            Span<RenderableWall> finalWalls = new RenderableWall[rotatedWalls.Length];

            int i = 0;
            for (int s = 0; s < bunches.Length; s++)
            {
                Span<RenderableWall> visible = rotatedWalls[bunches[s]];

                CullWallsFromBunch(ref visible, parentPortalWallsToOcclude);

                for (int v = 0; v < visible.Length; v++)
                {
                    finalWalls[i++] = visible[v];
                }
            }

            return finalWalls[..i];
        }

        public void CullWallsFromBunch(ref Span<RenderableWall> walls, Span<RenderableWall> parentPortalWallsToOcclude)
        {
            for (int i = 0; i < parentPortalWallsToOcclude.Length; i++)
            {
                FilterParentPortalWall(ref walls, parentPortalWallsToOcclude[i]);
            }

            if (walls.Length <= 1)
            {
                return;
            }

            walls.Sort(wallComparer);
            CullWallsBasedOnVisibility(ref walls);
        }

        public void CalculateWallPlane(RenderableWall wall, float yCeil, float yFloor, float yaw)
        {
            // calculate the x, y for the wall on the screen for both points
            float rx1 = wall.R1.X;
            float ry1 = wall.R1.Y;
            float rx2 = wall.R2.X;
            float ry2 = wall.R2.Y;

            float yLeftCeil, yLeftFloor, yRightCeil, yRightFloor;
            float scale = width * -EngineConstants.HeightToWidthRatio;
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;

            float xLeft = halfWidth - rx1 / ry1 * scale;
            float xRight = halfWidth - rx2 / ry2 * scale;

            // order left to right
            if (xLeft > xRight)
            {
                (xLeft, xRight) = (xRight, xLeft);

                (rx1, rx2) = (rx2, rx1);
                (ry1, ry2) = (ry2, ry1);

                (wall.R1, wall.R2) = (wall.R2, wall.R1);

                // wall.Flipped = true;
            }

            // part of the wall is in the back
            if (ry1 <= 0f || ry2 <= 0f)
            {
                float d2x = rx2 - rx1;
                float d2y = ry2 - ry1;

                bool intersectsL = MathFormulas.TryGetSegmentIntersectionZero2(-EngineConstants.CameraPlaneX, rx1, ry1, d2x, d2y,
                    out float xDistanceL, out float yDistanceL);

                bool intersectsR = MathFormulas.TryGetSegmentIntersectionZero2(EngineConstants.CameraPlaneX, rx2, ry2, -d2x, -d2y,
                    out float xDistanceR, out float yDistanceR);

                if (intersectsL && intersectsR)
                {
                    rx1 = xDistanceL;
                    ry1 = yDistanceL;

                    rx2 = xDistanceR;
                    ry2 = yDistanceR;

                    xLeft = 0;
                    xRight = width - 1;

                    wall.IntersectsView = true;
                    wall.Flipped = true;
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

                    wall.Flipped = true;
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

                (wall.R1, wall.R2) = (wall.R2, wall.R1);

                wall.Flipped = !wall.Flipped;
            }

            wall.IntersectsView |= MathFormulas.CalculatePlaneIntersectionsForWall(width, xLeft, xRight, ref rx1, ref ry1, ref rx2, ref ry2);

            if (wall.IntersectsView)
            {
                yLeftCeil = halfHeight - (yCeil / ry1 - yaw) * height;
                yLeftFloor = halfHeight - (yFloor / ry1 - yaw) * height;
                yRightCeil = halfHeight - (yCeil / ry2 - yaw) * height;
                yRightFloor = halfHeight - (yFloor / ry2 - yaw) * height;

                wall.C1 = new(rx1, ry1);
                wall.C2 = new(rx2, ry2);

                wall.XLeft = float.ConvertToIntegerNative<int>(xLeft);
                wall.XRight = float.ConvertToIntegerNative<int>(xRight);
                wall.YLeftCeil = float.ConvertToIntegerNative<int>(yLeftCeil);
                wall.YLeftFloor = float.ConvertToIntegerNative<int>(yLeftFloor);
                wall.YRightCeil = float.ConvertToIntegerNative<int>(yRightCeil);
                wall.YRightFloor = float.ConvertToIntegerNative<int>(yRightFloor);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Clamp(ref float xLeft, ref float xRight)
            {
                xLeft = Math.Clamp(xLeft, 0f, width - 1f);
                xRight = Math.Clamp(xRight, 0f, width - 1f);
            }
        }

        public static RenderableWall RotateWall(RenderableWall wall, float psin, float pcos, float px, float py)
        {
            // Vertex Points (Wall)
            // point 1 (vx1, vy1), point 2 (vx2, vy2)
            float vx1 = wall.R1.X;
            float vy1 = wall.R1.Y;
            float vx2 = wall.R2.X;
            float vy2 = wall.R2.Y;

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

            float dx = vx1 - vx2;
            float dy = vy1 - vy2;
            float length = MathF.Sqrt((dx * dx) + (dy * dy));

            return new RenderableWall(wall.Line, new Point(rx1, ry1), new Point(rx2, ry2), wall.Sector, wall.Neighbor)
            {
                Length = length,
                Bunch = wall.Bunch
            };
        }
    }
}
