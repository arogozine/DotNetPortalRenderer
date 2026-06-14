using SoftwareRendererModels;
using System.Numerics;
using static RenderingEngine.Engine.SharedHelpers;

namespace RenderingEngine.Engine
{
    /// <summary>
    /// Calculates and determines which walls can be rendered
    /// </summary>
    internal sealed class WallHelper
    {
        private readonly int width;
        private readonly int height;
        private readonly bool[] visibility;
        private readonly WallComparer wallComparer;
        private PortalPlayerSnapshot? _player;

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
            _player = player;
        }

        public Span<RenderableWall> DetermineWallsToRender(RenderableSector sector, Span<RenderableWall> portalWallsToOcclude, NeighborsToRender sectorInfo, PortalPlayerSnapshot player)
        {
            Span<RenderableWall> rotatedWalls = CalculateRotatedWallsRelativeToPlayer(sector, player, sectorInfo);

            rotatedWalls = CullWallsOutsideOfWindow(rotatedWalls, sectorInfo);

            Span<Range> bunches = BreakUpIntoBunches(rotatedWalls);
            Span<RenderableWall> result = CullHiddenWallsAndCombineBunches(bunches, rotatedWalls, portalWallsToOcclude);

            // we already sorted and culled bunches themselves, thus
            // if there is just one bunch, no need to sort and cull again
            if (bunches.Length <= 1)
            {
                return result;
            }

            result.Sort(wallComparer);
            result = CullWallsBasedOnVisibility(result);

            return result;
        }

        private static Span<RenderableWall> CullWallsOutsideOfWindow(Span<RenderableWall> rotatedWalls, NeighborsToRender sectorInfo)
        {
            // if the horizontal window is smaller then [0..width] (ex, a portal far away)
            // we can cull all walls that won't show up to increase performance significantly

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

        private Span<RenderableWall> CullWallsBasedOnVisibility(Span<RenderableWall> orderedWalls)
        {
            // each wall is visible from XLeft to XRight
            // given a span of sorted walls closest to furthest,
            // we can determine if further away walls can still be rendered

            Span<bool> visibility = this.visibility;
            visibility.Fill(true);

            int j = 0;
            for (int i = 0; i < orderedWalls.Length; i++)
            {
                RenderableWall wall = orderedWalls[i];
                int xLeft = wall.XLeft;
                int xRight = wall.XRight;

                int xRightExclusive = Math.Min(xRight + 1, width);
                Span<bool> subspan = visibility[xLeft..xRightExclusive];
                bool visible = subspan.Contains(true);
                subspan.Clear();

                if (visible)
                {
                    orderedWalls[j] = wall;
                    j++;
                }
            }

            return orderedWalls[..j];
        }

        public Span<RenderableWall> CalculateRotatedWallsRelativeToPlayer(RenderableSector sector, PortalPlayerSnapshot player, NeighborsToRender sectorInfo)
        {
            float pSin = player.Sin;
            float pCos = player.Cos;
            float px = player.X;
            float py = player.Y;

            for (int i = 0; i < sector.Walls.Length; i++)
            {
                RenderableWall wall = sector.Walls[i];

                wall.R1 = wall.PointA;
                wall.R2 = wall.PointB;
                wall.Flipped = false;
            }

            bool flipped = sectorInfo.MirrorWall is not null && sectorInfo.ParentWalls.Contains(sectorInfo.MirrorWall);

            if (flipped)
            {
                MirrorWalls(sector.Walls, sectorInfo.MirrorWall!);
            }

            Span<RenderableWall> rotatedWalls = RotateSectorWallsRelativeToPlayer(sector, pSin, pCos, px, py);

            rotatedWalls = FilterOutWallsBehindPlayer(rotatedWalls, flipped);
            CalculateWallPlanes(rotatedWalls, player);
            rotatedWalls = FilterOutWallsOutsideView(rotatedWalls);

            return rotatedWalls;
        }

        public static void MirrorWalls(Span<RenderableWall> rotatedWalls, RenderableWall mirroredWall)
        {
            for (int i = 0; i < rotatedWalls.Length; i++)
            {
                RenderableWall wall = rotatedWalls[i];

                if (wall.Id == mirroredWall.Id)
                {
                    continue;
                }

                wall.R1 = MathFormulas.ReflectPoint(wall.PointA, mirroredWall.PointA, mirroredWall.PointB);
                wall.R2 = MathFormulas.ReflectPoint(wall.PointB, mirroredWall.PointA, mirroredWall.PointB);
                wall.Flipped = true;
            }
        }

        private readonly HashSet<RenderableSector> connectingSectors = [];

        public void CalculateConnectingSectorsForSlope(PortalPlayerSnapshot player, ReadOnlySpan<RenderableSector> sectors, RenderableSector sector)
        {
            float pSin = player.Sin;
            float pCos = player.Cos;
            float px = player.X;
            float py = player.Y;

            connectingSectors.Clear();

            for (int i = 0; i < sector.Walls.Length; i++)
            {
                RenderableWall wall = sector.Walls[i];

                if (wall.IsPortal && wall.Neighbor != sector.Id)
                {
                    var n = sectors[wall.Neighbor!.Value];

                    if (connectingSectors.Add(n))
                    {
                        RenderableWall firstWall = n.Walls[0];

                        firstWall.R1 = firstWall.PointA;
                        firstWall.R2 = firstWall.PointB;

                        _ = RotateWall(firstWall, pSin, pCos, px, py);
                    }
                }
            }
        }

        public static void AssignBunches(scoped ReadOnlySpan<RenderableSector> sectors)
        {
            Queue<RenderableWall> assignedWallsQueue = [];

            for (int s = 0; s < sectors.Length; s++)
            {
                RenderableSector sector = sectors[s];
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

        private static Span<RenderableWall> FilterParentPortalWall(Span<RenderableWall> rotatedWalls, RenderableWall parentSectorWall)
        {
            for (int i = 0; i < rotatedWalls.Length; i++)
            {
                RenderableWall wall = rotatedWalls[i];

                if (wall.Id == parentSectorWall.Id)
                {
                    for (int j = i + 1; j < rotatedWalls.Length; j++, i++)
                    {
                        rotatedWalls[i] = rotatedWalls[j];
                    }

                    rotatedWalls = rotatedWalls[0..i];
                    return rotatedWalls;
                }
            }

            return rotatedWalls;
        }

        private RenderableWall[] _rotatedWalls = new RenderableWall[32];

        public Span<RenderableWall> RotateSectorWallsRelativeToPlayer(RenderableSector sector, float pSin, float pCos, float px, float py)
        {
            ReadOnlySpan<RenderableWall> walls = sector.Walls;

            if (_rotatedWalls.Length < walls.Length)
            {
                Array.Resize(ref _rotatedWalls, walls.Length);
            }

            Span<RenderableWall> rotatedWalls = _rotatedWalls.AsSpan()[..walls.Length];

            // Rotate relative to player
            for (int i = 0; i < walls.Length; i++)
            {
                rotatedWalls[i] = RotateWall(walls[i], pSin, pCos, px, py);
            }

            return rotatedWalls;
        }

        private Range[] _bunches = new Range[32];

        public Span<Range> BreakUpIntoBunches(scoped ReadOnlySpan<RenderableWall> rotatedWalls)
        {
            // a bunch is a set of connected walls
            // we figure out the range of each bunch here

            int bunchLength = rotatedWalls.Length > 0 ? rotatedWalls.Length : 1;

            if (_bunches.Length < rotatedWalls.Length)
            {
                Array.Resize(ref _bunches, rotatedWalls.Length);
            }

            Span<Range> bunches = _bunches.AsSpan()[..bunchLength];

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
                    if (current.Bunch != next.Bunch)
                    {
                        bunches[bunchCount++] = subsetStart..b;
                        bunches[bunchCount++] = b..rotatedWalls.Length;
                    }
                    else
                    {
                        bunches[bunchCount++] = subsetStart..rotatedWalls.Length;
                    }
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

        public static Span<RenderableWall> FilterOutWallsBehindPlayer(Span<RenderableWall> walls, bool flipped)
        {
            int j = 0;

            for (int i = 0; i < walls.Length; i++)
            {
                RenderableWall wall = walls[i];

                (float x1, float y1) = wall.R1;
                (float x2, float y2) = wall.R2;

                // In 2.5D Game Engines,
                // 1. Cull walls where both points are behind the player
                // 2. Cull walls where both points are to the right or to the left of the render cone
                // 3. Backface culling
                // See https://theforceengine.github.io/2020/05/16/DFRender1.html

                // wall fully behind the player
                if (y1 <= 0f && y2 <= 0f)
                {
                    continue;
                }

                if ((x1 < -y1 && x2 < -y2) || (x1 > y1 && x2 > y2))
                {
                    continue;
                }

                if (!wall.TwoSided && (flipped ? x2 * y1 > y2 * x1 : x2 * y1 < y2 * x1))
                {
                    continue;
                }

                walls[j] = wall;
                j++;
            }

            return walls[..j];
        }

        public void CalculateWallPlanes(scoped ReadOnlySpan<RenderableWall> walls, PortalPlayerSnapshot player)
        {
            float pz = player.Z;
            float yaw = player.Yaw;

            for (int i = 0; i < walls.Length; i++)
            {
                RenderableWall wall = walls[i];
                CalculateWallPlane(wall, pz, yaw);
            }
        }

        private static Span<RenderableWall> FilterOutWallsOutsideView(Span<RenderableWall> walls)
        {
            // we remove walls that do not intersect the view

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

                walls[j] = wall;
                j++;
            }

            return walls[..j];
        }

        private RenderableWall[] _visible = new RenderableWall[8];

        public Span<RenderableWall> CullHiddenWallsAndCombineBunches(
            scoped Span<Range> bunches, scoped Span<RenderableWall> rotatedWalls, ReadOnlySpan<RenderableWall> parentPortalWallsToOcclude)
        {
            
            if (rotatedWalls.Length > _visible.Length)
            {
                Array.Resize(ref _visible, rotatedWalls.Length);
            }

            Span<RenderableWall> finalWalls = _visible.AsSpan();
            finalWalls.Clear();

            int i = 0;
            for (int s = 0; s < bunches.Length; s++)
            {
                Span<RenderableWall> visible = rotatedWalls[bunches[s]];

                visible = CullWallsFromBunch(visible, parentPortalWallsToOcclude);

                for (int v = 0; v < visible.Length; v++)
                {
                    visible[v].Bunch = s;
                    finalWalls[i++] = visible[v];
                }
            }

            return finalWalls[..i];
        }

        public Span<RenderableWall> CullWallsFromBunch(Span<RenderableWall> walls, ReadOnlySpan<RenderableWall> parentPortalWallsToOcclude)
        {
            for (int i = 0; i < parentPortalWallsToOcclude.Length; i++)
            {
                walls = FilterParentPortalWall(walls, parentPortalWallsToOcclude[i]);
            }

            if (walls.Length <= 1)
            {
                return walls;
            }

            walls.Sort(wallComparer);

            return CullWallsBasedOnVisibility(walls);
        }

        public void CalculateWallPlane(RenderableWall wall, float pz, float yaw)
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

            wall.IntersectsView = false;

            /*
            // order left to right
            if (xLeft > xRight)
            {
                (xLeft, xRight) = (xRight, xLeft);

                (rx1, rx2) = (rx2, rx1);
                (ry1, ry2) = (ry2, ry1);

                (wall.R1, wall.R2) = (wall.R2, wall.R1);

                wall.Flipped = true;
            }
            */

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
                    // wall.Flipped = true;
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

                    //wall.Flipped = true;
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

            Clamp(ref xLeft, ref xRight);

            if (xLeft >= xRight)
            {
                wall.IntersectsView = false;
                return;
            }

            // order left to right
            if (xLeft > xRight)
            {
                (xLeft, xRight) = (xRight, xLeft);

                (rx1, rx2) = (rx2, rx1);
                (ry1, ry2) = (ry2, ry1);

                (wall.R1, wall.R2) = (wall.R2, wall.R1);

                //(yCeilA, yCeilB) = (yCeilB, yCeilA);
                //(yFloorA, yFloorB) = (yFloorB, yFloorA);

                //wall.Flipped = !wall.Flipped;
            }

            wall.IntersectsView |= MathFormulas.CalculatePlaneIntersectionsForWall(width, xLeft, xRight, ref rx1, ref ry1, ref rx2, ref ry2);

            if (wall.IntersectsView)
            {
                wall.C1 = new(rx1, ry1);
                wall.C2 = new(rx2, ry2);

                float sectorCeil = wall.Sector.Ceil;
                float sectorFloor = wall.Sector.Floor;

                sectorFloor -= pz;
                sectorCeil -= pz;

                yLeftCeil = halfHeight - (sectorCeil / ry1 - yaw) * height;
                yLeftFloor = halfHeight - (sectorFloor / ry1 - yaw) * height;
                yRightCeil = halfHeight - (sectorCeil / ry2 - yaw) * height;
                yRightFloor = halfHeight - (sectorFloor / ry2 - yaw) * height;

                wall.XLeft = float.ConvertToIntegerNative<int>(xLeft);
                wall.XRight = float.ConvertToIntegerNative<int>(xRight);
                wall.YLeftCeil = float.ConvertToIntegerNative<int>(yLeftCeil);
                wall.YLeftFloor = float.ConvertToIntegerNative<int>(yLeftFloor);
                wall.YRightCeil = float.ConvertToIntegerNative<int>(yRightCeil);
                wall.YRightFloor = float.ConvertToIntegerNative<int>(yRightFloor);

                (float yFloorA, float yCeilA, float yFloorB, float yCeilB) = MathFormulas.CalculateSlopedFloorCeiling(wall.Sector, wall, false);

                yFloorA -= pz;
                yCeilA -= pz;
                yFloorB -= pz;
                yCeilB -= pz;

                yLeftCeil = halfHeight - (yCeilA / ry1 - yaw) * height;
                yLeftFloor = halfHeight - (yFloorA / ry1 - yaw) * height;
                yRightCeil = halfHeight - (yCeilB / ry2 - yaw) * height;
                yRightFloor = halfHeight - (yFloorB / ry2 - yaw) * height;

                wall.YLeftCeilSloped = float.ConvertToIntegerNative<int>(yLeftCeil);
                wall.YLeftFloorSloped = float.ConvertToIntegerNative<int>(yLeftFloor);
                wall.YRightCeilSloped = float.ConvertToIntegerNative<int>(yRightCeil);
                wall.YRightFloorSloped = float.ConvertToIntegerNative<int>(yRightFloor);
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

            wall.R1 = new Vector2(rx1, ry1);
            wall.R2 = new Vector2(rx2, ry2);
            wall.Length = length;

            return wall;
        }
    }
}
