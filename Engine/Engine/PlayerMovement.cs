using RenderingEngine.Models;
using System.Diagnostics;

namespace RenderingEngine.Engine
{
    internal static class PlayerMovement
    {
        public static void MovePlayer(Player player, ReadOnlySpan<Sector> sectors, float dx, float dy)
        {
            int oldSectorId = player.Sector;
            int? sector = GetNewSector(player, sectors, dx, dy);

            // prevent moving outside map
            if (sector is null)
            {
                return;
            }

            (float x, float y, float z) = player.Where;

            if (oldSectorId == sector.Value)
            {
                player.Where = (x + dx, y + dy, z);
                SnapPlayerZ(player, sectors[player.Sector]);
            }
            else
            {
                player.Sector = sector.Value;
                Sector oldSector = sectors[oldSectorId];
                Sector newSector = sectors[sector.Value];
                z += newSector.Floor - oldSector.Floor;

                player.Where = (x + dx, y + dy, z);
            }
        }

        private static void SnapPlayerZ(Player player, Sector sector)
        {
            (float x, float y, float z) = player.Where;

            if (z < sector.Floor)
            {
                player.Where = (x, y, sector.Floor + 6);
            }
        }


        public static int? GetNewSector(Player player, ReadOnlySpan<Sector> sectors, float dx, float dy)
        {
            XyzTuple location = (player.Where.X + dx, player.Where.Y + dy, player.Where.Z);

            for (int s = 0; s < sectors.Length; s++)
            {
                Sector sector = sectors[s];
                Wall[] walls = sector.Walls;

                if (IsPointInSector(walls, location))
                {
                    return s;
                }
            }

            return null;
        }

        public static bool IsPointInSector(Span<Wall> walls, XyzTuple point)
        {
            int intersections = 0;

            for (int i = 0; i < walls.Length; i++)
            {
                Wall wall = walls[i];

                float pointA_X = wall.X1;
                float pointA_Y = wall.Y1;
                float pointB_X = wall.X2;
                float pointB_Y = wall.Y2;

                // Check if point is on the same horizontal level as the edge's y-coordinates
                if (point.Y > MathF.Min(pointA_Y, pointB_Y) && point.Y <= MathF.Max(pointA_Y, pointB_Y))
                {
                    // Calculate the x-coordinate of the intersection of the ray with the edge
                    if (point.Y != pointA_Y && point.Y != pointB_Y)
                    {
                        float intersectX = pointA_X + (point.Y - pointA_Y) * (pointB_X - pointA_X) / (pointB_Y - pointA_Y);
                        if (intersectX > point.X)
                        {
                            intersections++;
                        }
                    }
                }
            }

            // If the number of intersections is odd, the point is inside the polygon
            return intersections % 2 != 0;
        }
    }
}
