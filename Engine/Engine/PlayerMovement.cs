using RenderingEngine.Models;

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
                Sector newSector = sectors[sector.Value];

                (float sectorFloor, _) = MathFormulas.CalculateZAtPoint(newSector, new Point(x, y), true);

                z = sectorFloor + EngineConstants.PlayerHeight;

                player.Where = (x + dx, y + dy, z);
            }
        }

        private static void SnapPlayerZ(Player player, Sector sector)
        {
            (float x, float y, float z) = player.Where;

            (float sectorFloor, _) = MathFormulas.CalculateZAtPoint(sector, new Point(x, y), true);

            if (z < sectorFloor || sector.Settings.Sloped)
            {
                player.Where = (x, y, EngineConstants.PlayerHeight + sectorFloor);
            }
        }


        public static int? GetNewSector(Player player, ReadOnlySpan<Sector> sectors, float dx, float dy)
        {
            Point location = new (player.Where.X + dx, player.Where.Y + dy);

            Sector playerSector = sectors[player.Sector];

            // only look at adjacent sectors
            var childSectors = new HashSet<int> { player.Sector };

            for (int i = 0; i < playerSector.Walls.Length; i++)
            {
                RenderableWall wall = playerSector.Walls[i];

                if (wall.IsPortal)
                {
                    _ = childSectors.Add(wall.Neighbor);
                }
            }

            foreach (int s in childSectors)
            {
                Sector sector = sectors[s];
                RenderableWall[] walls = sector.Walls;

                if (SharedHelpers.IsPointInPolygon(walls, location))
                {
                    return s;
                }
            }

            // expand search
            for (int s = 0; s < sectors.Length; s++)
            {
                Sector sector = sectors[s];
                RenderableWall[] walls = sector.Walls;

                if (SharedHelpers.IsPointInPolygon(walls, location))
                {
                    return s;
                }
            }

            return null;
        }
    }
}
