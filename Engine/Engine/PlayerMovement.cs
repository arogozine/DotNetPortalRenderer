using RenderingEngine.Tooling;
using SoftwareRendererModels;
using System.Numerics;

namespace RenderingEngine.Engine
{
    internal static class PlayerMovement
    {
        public static void MovePlayer(PlayerLocation player, RenderableMap renderableMap, float dx, float dy)
        {
            var sectors = renderableMap.Sectors;

            int oldSectorId = player.Sector;
            int? sectorId = GetNewSector(player, sectors, dx, dy);

            // prevent moving outside map
            if (sectorId is null)
            {
                return;
            }

            (float x, float y, float z) = player.Where;

            x += dx;
            y += dy;

            RenderableFloorSprite? floorSprite = CheckForFloorSpriteFloor(x, y, z, renderableMap);

            if (floorSprite is { })
            {
                player.Sector = sectorId.Value;
                RenderableSector sector = sectors[floorSprite.SectorId];
                float sectorFloor = sector.Floor;

                z = sectorFloor + floorSprite.Height + EngineConstants.PlayerHeight;
                player.Where = (x, y, z);

                return;
            }

            if (oldSectorId == sectorId.Value)
            {
                z = SnapPlayerZ(x, y, z, sectors[player.Sector]);
            }
            else
            {
                player.Sector = sectorId.Value;
                RenderableSector newSector = sectors[sectorId.Value];

                (z, _) = MathFormulas.CalculateZAtPoint(newSector, new Vector2(x, y), true);
                z += EngineConstants.PlayerHeight;
            }

            player.Where = (x, y, z);
        }

        private static RenderableFloorSprite? CheckForFloorSpriteFloor(
            float x, float y, float z, RenderableMap renderableMap)
        {
            Vector2 location = new(x, y);

            ReadOnlySpan<RenderableSprite> sprites = renderableMap.Sprites;
            ReadOnlySpan<RenderableSector> sectors = renderableMap.Sectors;

            for (int i = 0; i < sprites.Length; i++)
            {
                RenderableSprite sprite = sprites[i];

                if (sprite is RenderableFloorSprite floorSprite)
                {
                    float spriteZ = sectors[sprite.SectorId].Floor + sprite.Height;

                    if (z >= spriteZ && SharedHelpers.IsPointInPolygon(floorSprite, location))
                    {
                        return floorSprite;
                    }
                }
            }

            return null;
        }

        private static float SnapPlayerZ(float x, float y, float z, RenderableSector sector)
        {
            (float sectorFloor, _) = MathFormulas.CalculateZAtPoint(sector, new Vector2(x, y), true);

            if (z < sectorFloor || sector.Settings.Sloped)
            {
                return EngineConstants.PlayerHeight + sectorFloor;
            }

            return z;
        }

        public static int? GetNewSector(PlayerLocation player, ReadOnlySpan<RenderableSector> sectors, float dx, float dy)
        {
            Vector2 location = new (player.Where.X + dx, player.Where.Y + dy);

            RenderableSector playerSector = sectors[player.Sector];

            // only look at adjacent sectors

            HashSet<int> childSectors = ObjectPool.HashSet;
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
                if (!childSectors.Contains(sector.Id) && SharedHelpers.IsPointInPolygon(sector.Walls, location))
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
