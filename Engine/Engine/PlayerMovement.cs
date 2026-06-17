using RenderingEngine.Tooling;
using SoftwareRendererModels;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal static class PlayerMovement
    {
        public static void MovePlayer(PlayerLocation player, ReadOnlySpan<RenderableSector> sectors, float dx, float dy)
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
                RenderableSector newSector = sectors[sector.Value];

                (float sectorFloor, _) = MathFormulas.CalculateZAtPoint(newSector, new Vector2(x, y), true);

                z = sectorFloor + EngineConstants.PlayerHeight;

                player.Where = (x + dx, y + dy, z);
            }
        }

        private static void SnapPlayerZ(PlayerLocation player, RenderableSector sector)
        {
            (float x, float y, float z) = player.Where;

            (float sectorFloor, _) = MathFormulas.CalculateZAtPoint(sector, new Vector2(x, y), true);

            if (z < sectorFloor || sector.Settings.Sloped)
            {
                player.Where = (x, y, EngineConstants.PlayerHeight + sectorFloor);
            }
        }


        public static int? GetNewSector(PlayerLocation player, ReadOnlySpan<RenderableSector> sectors, float dx, float dy)
        {
            Vector2 location = new (player.Where.X + dx, player.Where.Y + dy);

            RenderableSector playerSector = sectors[player.Sector];

            // only look at adjacent sectors

            var childSectors = ObjectPool.HashSet;
            childSectors.Clear();
            _ = childSectors.Add(player.Sector);

            for (int i = 0; i < playerSector.Walls.Length; i++)
            {
                RenderableWall wall = playerSector.Walls[i];

                if (wall.IsPortal)
                {
                    Debug.Assert(wall.Neighbor != null);
                    _ = childSectors.Add(wall.Neighbor.Value);
                }
            }

            foreach (int s in childSectors)
            {
                RenderableSector sector = sectors[s];
                RenderableWall[] walls = sector.Walls;

                if (SharedHelpers.IsPointInPolygon(walls, location))
                {
                    return s;
                }
            }

            // expand search
            for (int s = 0; s < sectors.Length; s++)
            {
                RenderableSector sector = sectors[s];
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
