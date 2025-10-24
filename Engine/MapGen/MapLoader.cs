using RenderingEngine.DoomMapLoader;
using RenderingEngine.Engine;
using RenderingEngine.Models;
using System.Text.Json;

namespace RenderingEngine.MapGen
{
    public static class MapLoader
    {
        private static void StripInvalidNeighbors(Map map)
        {
            int sectorCount = map.Sectors.Count;

            Dictionary<int, int> sectorIdToIndex = map.Sectors.Select((x, i) => new { x, i }).ToDictionary(
                x => x.x.Id, x => x.i);

            foreach (var sector in map.Sectors)
            {
                foreach (var wall in sector.Walls)
                {
                    if (wall.SectorTo is int sectorTo)
                    {
                        if (sectorIdToIndex.TryGetValue(sectorTo, out int sectorIndex))
                        {
                            wall.SectorTo = sectorIndex;
                        }
                        else
                        {
                            wall.SectorTo = null;
                        }
                    }
                }
            }
        }

        private static List<Line> SortMapWalls(List<Line> walls)
        {
            walls = walls
                .Where(x => x.PointA.X != x.PointB.X || x.PointA.Y != x.PointB.Y)
                .ToList();

            for (int i = 0, j = 1; j < walls.Count; i++, j++)
            {
                Line wall = walls[i];
                Line wallNext = walls[j];

                for (; j < walls.Count; j++)
                {
                    wallNext = walls[j];

                    if (wall.PointB == wallNext.PointA)
                    {
                        (walls[i + 1], walls[j]) = (walls[j], walls[i + 1]);
                        break;
                    }

                    if (wall.PointB == wallNext.PointB)
                    {
                        (wall.PointA, wall.PointB) = (wall.PointB, wall.PointA);
                        (walls[i + 1], walls[j]) = (walls[j], walls[i + 1]);
                        break;
                    }
                }

                j = i + 1;
            }

            return walls;
        }

        internal static (Player player, Sector[] sectors) LoadData()
        {
            Map map = WadReader.ExtractDoomMap("MAP01");//("MAP26");
            StripInvalidNeighbors(map);

            for (int i = 0; i < map.Sectors.Count; i++)
            {
                var sector = map.Sectors[i];
                sector.Walls = SortMapWalls(sector.Walls);
            }

            var sectors = map.Sectors.Select(ParseMapSector).ToArray();

            return (map.Player, sectors);
        }

        static Sector ParseMapSector(MapSector x)
        {
            Wall[] vertex = new Wall[x.Walls.Count];

            for (int i = 0; i < x.Walls.Count; i++)
            {
                Line v = x.Walls[i];
                vertex[i] = new Wall(v,
                    v.PointA, v.PointB,
                    v.SectorTo
                );
            }

            var sector = new Sector
            {
                HasSkybox = x.HasSkybox,
                FloorTexture = x.FloorTexture,
                CeilTexture = x.CeilingTexture,
                Ceil = x.Ceiling,
                Floor = x.Floor,
                Walls = vertex
            };

            return sector;

        }
    }
}
