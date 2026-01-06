using BuildAssetLoader;
using BuildAssetLoader.Texture;
using DoomAssetLoader.Wad;
using RenderingEngine.DoomMapLoader;
using RenderingEngine.Engine;
using RenderingEngine.Models;

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

        internal static Map LoadDoomEngineMap(string mapName, string iwad, string? pwad)
        {
            WadFile wadFile = WadReader.LoadWad(iwad, loadMaps: pwad is null, loadTextures: true, mapName: mapName);
            WadReader.ExtractAllTextures(wadFile);

            if (pwad is not null)
            {
                WadFile wadFile2 = WadReader.LoadWad(pwad, loadMaps: true, loadTextures: true, mapName: mapName);
                _ = wadFile2.LoadRequired(wadFile);
                WadReader.ExtractAllTextures(wadFile2);

                return WadReader.LoadDoomMap(wadFile2, mapName);

            }
            else
            {
                return WadReader.LoadDoomMap(wadFile, mapName);
            }
        }

        internal static Map LoadBuildEngineMap(string mapName, string grpPath, string palettePath)
        {
            PaletteFile pal = BuildFileLoader.LoadPalFile(palettePath);
            GrpFile grp = BuildFileLoader.LoadGrpFile(grpPath);

            GrpReader.ExtractAllTextures(grp, pal);

            return GrpReader.LoadBuildMap(grp, mapName);
        }

        internal static (Player player, Sector[] sectors, RenderableSprite[] sprites) LoadData(Arguments arguments)
        {
            Map map;

            if (arguments.IWad is not null)
            {
                map = LoadDoomEngineMap(arguments.Map, arguments.IWad, arguments.PWad);
            }
            else
            {
                map = LoadBuildEngineMap(arguments.Map, arguments.Grp!, arguments.Palette!);
            }

            StripInvalidNeighbors(map);

            for (int i = 0; i < map.Sectors.Count; i++)
            {
                var sector = map.Sectors[i];
                sector.Walls = SortMapWalls(sector.Walls);
            }

            RenderableSprite[] sprites = map.Sprites.Select(ParseSprite).ToArray();
            Sector[] sectors = map.Sectors.Select(ParseMapSector).ToArray();

            if (arguments.IWad is not null)
            {
                SpriteHelper.AssignSectors(sprites, sectors);
            }

            WallHelper.AssignBunches(sectors);

            return (map.Player, sectors, sprites);
        }

        static RenderableSprite ParseSprite(Sprite sprite)
        {
            return new RenderableSprite
            {
                Sprite = sprite
            };
        }

        static Sector ParseMapSector(MapSector x)
        {
            RenderableWall[] vertex = new RenderableWall[x.Walls.Count];

            var sector = new Sector
            {
                Id = x.Id,
                FloorTexture = x.FloorTexture,
                CeilTexture = x.CeilingTexture,
                Ceil = x.Ceiling,
                Floor = x.Floor,
                LightLevel = (byte)(x.LightLevel == 256 ? byte.MaxValue : x.LightLevel),
                Walls = vertex,
                RotationCeiling = x.RotationCeiling ?? 0f,
                RotationFloor = x.RotationFloor ?? 0f
            };

            for (int i = 0; i < x.Walls.Count; i++)
            {
                Line v = x.Walls[i];
                vertex[i] = new RenderableWall(v,
                    v.PointA, v.PointB,
                    sector,
                    v.SectorTo
                );
            }

            return sector;

        }
    }
}
