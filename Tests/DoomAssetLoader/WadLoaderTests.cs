using DoomAssetLoader;
using DoomAssetLoader.Wad;

namespace Tests;

public class WadLoaderTests
{
    [SkipIfDirectoryNotSetFact(TestDataDirectory.Doom)]
    public void LoadWad_Doom2Wad_ReturnsIwadWithLumps()
    {
        string path = Path.Combine(TestConfiguration.Current.DoomDirectory, "doom2.wad");

        var loader = new WadLoader(path)
        {
            LoadMaps = true,
            LoadTextures = true,
            LoadOther = true
        };

        WadFile wadFile = loader.LoadWad();

        Assert.Equal(WadType.IWAD, wadFile.Type);
        Assert.NotEmpty(wadFile.Lumps);
    }

    [SkipIfDirectoryNotSetFact(TestDataDirectory.Doom)]
    public void LoadWad_Doom2Wad_LoadsMap01Lumps()
    {
        string path = Path.Combine(TestConfiguration.Current.DoomDirectory, "doom2.wad");

        var loader = new WadLoader(path)
        {
            LoadMaps = true,
            MapToLoad = "MAP01"
        };

        WadFile wadFile = loader.LoadWad();

        Assert.NotNull(wadFile.GetMapLump("MAP01", LumpType.Things));
        Assert.NotNull(wadFile.GetMapLump("MAP01", LumpType.LineDefs));
        Assert.NotNull(wadFile.GetMapLump("MAP01", LumpType.Sectors));
        Assert.All(wadFile.Lumps, static lump => Assert.Equal("MAP01", lump.MapName));
    }

    [SkipIfDirectoryNotSetFact(TestDataDirectory.Doom)]
    public void LoadWad_Doom2WadWithoutLoadMaps_ExcludesMapLumps()
    {
        string path = Path.Combine(TestConfiguration.Current.DoomDirectory, "doom2.wad");

        var loader = new WadLoader(path)
        {
            LoadMaps = false,
            LoadTextures = true,
            LoadOther = true
        };

        WadFile wadFile = loader.LoadWad();

        Assert.DoesNotContain(wadFile.Lumps, static lump => lump.IsMap);
    }

    [SkipIfDirectoryNotSetFact(TestDataDirectory.Doom)]
    public void LoadWad_Doom2WadWithLoadTextures_LoadsPlayPalAndPNames()
    {
        string path = Path.Combine(TestConfiguration.Current.DoomDirectory, "doom2.wad");

        var loader = new WadLoader(path)
        {
            LoadTextures = true
        };

        WadFile wadFile = loader.LoadWad();

        Assert.NotNull(wadFile[LumpType.PlayPal]);
        Assert.NotNull(wadFile[LumpType.PNames]);
    }
}
