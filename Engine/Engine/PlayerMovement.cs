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
            Vector2 oldLocation = new(player.Where.X, player.Where.Y);
            Vector2 newLocation = new(player.Where.X + dx, player.Where.Y + dy);

            RenderableSector startSector = sectors[player.Sector];

            // prevent moving across solid lines
            if (dx != 0 || dy != 0)
            {
                foreach (RenderableWall w in startSector.Walls)
                {
                    if (w.IsPortal && (w.Traversable && !w.IsMirror))
                    {
                        continue;
                    }

                    if (SharedHelpers.DoSegmentsIntersect(oldLocation, newLocation, w.PointA, w.PointB))
                    {
                        return null;
                    }
                }
            }

            // BFS Search
            HashSet<int> checkedSectors = ObjectPool.HashSet.GetOrCreate();
            Queue<int> uncheckedSectorsQueue = ObjectPool.Queue.GetOrCreate();

            checkedSectors.Clear();
            uncheckedSectorsQueue.Clear();
            uncheckedSectorsQueue.Enqueue(player.Sector);

            while (uncheckedSectorsQueue.TryDequeue(out int i))
            {
                RenderableSector currentSector = sectors[i];

                if (SharedHelpers.IsPointInPolygon(currentSector.Walls, newLocation))
                {
                    return i;
                }

                foreach (RenderableWall w in currentSector.Walls)
                {
                    if (!w.IsPortal)
                    {
                        continue;
                    }

                    if (checkedSectors.Add(w.Neighbor!.Value))
                    {
                        uncheckedSectorsQueue.Enqueue(w.Neighbor.Value);
                    }
                }
            }

            return null;
        }
    }
}
