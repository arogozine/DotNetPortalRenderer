using RenderingEngine.Models;
using RenderingEngine.Models.Json;
using System.Text.Json;

namespace RenderingEngine.MapGen
{
    public static class MapLoader
    {
        public static async Task<Map?> LoadMapAsync(string filePath, CancellationToken cancellationToken = default)
        {
            using FileStream fileStream = File.OpenRead(filePath);
            Map? map = await JsonSerializer.DeserializeAsync(fileStream, MapJsonContext.Default.Map, cancellationToken: cancellationToken);
            return map;
        }

        public static Map? LoadMap(string filePath, CancellationToken cancellationToken = default)
        {
            using FileStream fileStream = File.OpenRead(filePath);
            Map? map = JsonSerializer.Deserialize(fileStream, MapJsonContext.Default.Map);
            return map;
        }
        private static void StripInvalidNeighbors(Map map)
        {
            int sectorCount = map.Sectors.Count;

            Dictionary<int, int> sectorIdToIndex = map.Sectors.Select((x, i) => new { x, i }).ToDictionary(
                x => x.x.SectorId, x => x.i);

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

        public static async Task WriteAsync(Map map, string filePath)
        {
            using FileStream fileStream = File.OpenWrite(filePath);
            await JsonSerializer.SerializeAsync(fileStream, map, MapJsonContext.Default.Map);
        }

        private static Map GenerateMap()
        {
            var generator = new MapGenerator();
            generator.AddPlayer((2, 6, 6), 0);

            int sector1 = generator.AddSector(0, 20);
            
            int sector2 = generator.AddSector(0, 24);
            int sector3 = generator.AddSector(10, 24);
            int sector4 = generator.AddSector(1, 20);
            int sector5 = generator.AddSector(2, 16);
            int sector6 = generator.AddSector(4, 14);
            int sector7 = generator.AddSector(4, 14);

            int a1 = generator.AddWall(sector1, (0, 0), (10, 0));
            int b1 = generator.AddWall(sector1, (10, 0), (10, 10), sector2);
            int c1 = generator.AddWall(sector1, (10, 10), (0, 10), sector4);
            int d1 = generator.AddWall(sector1, (0, 10), (0, 0));


            
            // x + 10
            int a2 = generator.AddWall(sector2, (10, 0), (20, 0));
            int b2 = generator.AddWall(sector2, (20, 0), (20, 10), sector3);
            int c2 = generator.AddWall(sector2, (20, 10), (10, 10));
            int d2 = generator.AddWall(sector2, (10, 10), (10, 0), sector1);


            // x + 20
            int a3 = generator.AddWall(sector3, (20, 0), (30, 0));
            int b3 = generator.AddWall(sector3, (30, 0), (30, 10));
            int c3 = generator.AddWall(sector3, (30, 10), (20, 10));
            int d3 = generator.AddWall(sector3, (20, 10), (20, 0), sector2);

            int? sector4Pillar = null;
            int e4 = generator.AddWall(sector4, (4, 14), (6, 14), sector7);
            int f4 = generator.AddWall(sector4, (6, 14), (6, 16), sector7);
            int g4 = generator.AddWall(sector4, (6, 16), (4, 16), sector7);
            int h4 = generator.AddWall(sector4, (4, 16), (4, 14), sector7);

            generator.AddWall(sector7, (4, 14), (6, 14), sector4);
            generator.AddWall(sector7, (6, 14), (6, 16), sector4);
            generator.AddWall(sector7, (6, 16), (4, 16), sector4);
            generator.AddWall(sector7, (4, 16), (4, 14), sector4);

            int a4 = generator.AddWall(sector4, (0, 10), (0, 20));
            int b4 = generator.AddWall(sector4, (0, 20), (10, 20), sector5);
            int c4 = generator.AddWall(sector4, (10, 20), (10, 10));
            int d4 = generator.AddWall(sector4, (10, 10), (0, 10), sector1);

            int a5 = generator.AddWall(sector5, (0, 20), (0, 30));
            int b5 = generator.AddWall(sector5, (0, 30), (10, 30));
            int c5 = generator.AddWall(sector5, (10, 30), (10, 20));
            int d5 = generator.AddWall(sector5, (10, 20), (0, 20), sector4);

            // triangle pillar
            int e5 = generator.AddWall(sector5, (1, 22), (1, 24), sector6);
            int f5 = generator.AddWall(sector5, (1, 24), (7, 23), sector6);
            int g5 = generator.AddWall(sector5, (7, 23), (1, 22), sector6);

            int a6 = generator.AddWall(sector6, (1, 22), (1, 24), sector5);
            int b6 = generator.AddWall(sector6, (1, 24), (7, 23), sector5);
            int c6 = generator.AddWall(sector6, (7, 23), (1, 22), sector5);


            return generator.GetMap();
        }

        internal static (Player player, Sector[] sectors) LoadData()
        {
            // var map = GenerateMap();
            var map = LoadMap("C:\\Users\\Alexa\\source\\repos\\DoomStruct\\src\\test\\resources\\test.json")!;


            StripInvalidNeighbors(map);

            for (int i = 0; i < map.Sectors.Count; i++)
            {
                var sector = map.Sectors[i];
                sector.Walls = SortMapWalls(sector.Walls);
            }

            Player player = new Player
            {
                Angle = map.Player.Angle,
                Sector = 4,
                Where = (8.251516f, 26.0253067f, 8f)
                // (1, 1, map.Player.ZPosition)
            };

            var sectors = map.Sectors.Select(ParseMapSector).ToArray();

            return (player, sectors);
        }

        static Sector ParseMapSector(MapSector x)
        {
            Wall[] vertex = new Wall[x.Walls.Count];

            for (int i = 0; i < x.Walls.Count; i++)
            {
                Line v = x.Walls[i];
                vertex[i] = new Wall(
                    v.PointA.X, v.PointA.Y,
                    v.PointB.X, v.PointB.Y,
                    v.SectorTo
                );
            }

            var sector = new Sector
            {

                Ceil = x.Ceiling,
                Floor = x.Floor,
                Walls = vertex
            };

            return sector;

        }
    }
}
