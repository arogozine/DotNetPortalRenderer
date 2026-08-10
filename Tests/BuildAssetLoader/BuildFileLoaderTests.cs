using BuildAssetLoader;

namespace Tests;

public class BuildFileLoaderTests
{
    [SkipIfDirectoryNotSetFact(TestDataDirectory.DukeNukem)]
    public void LoadGrpFile_Duke3dGrp_ReturnsFilesFromDirectory()
    {
        string path = Path.Combine(TestConfiguration.Current.DukeNukemDirectory, "DUKE3D.GRP");

        GrpFile grpFile = BuildFileLoader.LoadGrpFile(path);

        Assert.NotEmpty(grpFile.Files);
    }

    [SkipIfDirectoryNotSetFact(TestDataDirectory.DukeNukem)]
    public void LoadGrpFile_Duke3dGrp_ContainsKnownEntries()
    {
        string path = Path.Combine(TestConfiguration.Current.DukeNukemDirectory, "DUKE3D.GRP");

        GrpFile grpFile = BuildFileLoader.LoadGrpFile(path);

        Assert.True(grpFile.Files.ContainsKey("DEFS.CON"));
        Assert.True(grpFile.Files.ContainsKey("TABLES.DAT"));
        Assert.NotEmpty(grpFile.Files["TABLES.DAT"]);
    }
}
