using BuildAssetLoader;
using BuildAssetLoader.Texture;
using DoomAssetLoader.Wad;
using RenderingEngine.DoomMapLoader;
using RenderingEngine.Engine;
using SoftwareRendererModels;

namespace RenderingEngine.MapLoader
{
    public static class GameLoader
    {
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

        internal static (PlayerStart player, RenderableSector[] sectors, RenderableSprite[] sprites) LoadData(Arguments arguments)
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

            for (int i = 0; i < map.Sectors.Count; i++)
            {
                var sector = map.Sectors[i];
            }

            RenderableSprite[] sprites = map.Sprites.Select(ParseSprite).ToArray();
            RenderableSector[] sectors = map.Sectors.Select(ParseMapSector).ToArray();

            if (arguments.IWad is not null)
            {
                SpriteHelper.AssignSectors(sprites, sectors);
            }

            WallHelper.AssignBunches(sectors);

            return (map.Player, sectors, sprites);
        }

        static RenderableSprite ParseSprite(Sprite sprite)
        {
            if (sprite is WallSprite wallSprite)
            {
                return new RenderableWallSprite { Sprite = wallSprite };
            }
            else if (sprite is FloorSprite floorSprite)
            {
                return new RenderableFloorSprite { Sprite = floorSprite };
            }

            return new RenderableBasicSprite
            {
                Sprite = sprite
            };
        }

        internal static RenderableSector ParseMapSector(MapSector x)
        {
            RenderableWall[] vertex = new RenderableWall[x.Walls.Count];

            var sector = new RenderableSector
            {
                MapSector = x,
                Walls = vertex
            };

            for (int i = 0; i < x.Walls.Count; i++)
            {
                Line v = x.Walls[i];
                vertex[i] = new RenderableWall
                {
                    Sector = sector,
                    Line = v,
                };
            }

            return sector;

        }
    }
}
