using BuildAssetLoader;
using BuildAssetLoader.Texture;
using DoomAssetLoader.Wad;
using RenderingEngine.DoomMapLoader;
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

        internal static Map LoadBuildEngineMap(string mapName, string rootPath)
        {
            string grpPath = Path.Combine(rootPath, "DUKE3D.GRP");
            string palettePath = Path.Combine(rootPath, "PALETTE.DAT");

            string defsConPath = Path.Combine(rootPath, "DEFS.CON");
            string gameConPath = Path.Combine(rootPath, "GAME.CON");

            Dictionary<int, GrpReader.SpriteAngleRotation[]> spriteToAngleFrames = GrpReader.ExtractSpriteAngleInfo(defsConPath, gameConPath);


            PaletteFile pal = BuildFileLoader.LoadPalFile(palettePath);
            GrpFile grp = BuildFileLoader.LoadGrpFile(grpPath);

            GrpReader.ExtractAllTextures(grp, pal);

            return GrpReader.LoadBuildMap(grp, mapName, spriteToAngleFrames);
        }

        internal static FixedGameState LoadFixedGameState(Arguments arguments)
        {
            GameResourceType gameResourceType;
            Map map;

            if (arguments.IWad is not null)
            {
                map = LoadDoomEngineMap(arguments.Map, arguments.IWad, arguments.PWad);
                gameResourceType = GameResourceType.Doom;
            }
            else
            {
                map = LoadBuildEngineMap(arguments.Map, arguments.DukePath!);
                gameResourceType = GameResourceType.DukeNukem;
            }

            return new(map, gameResourceType);
        }
    }
}
