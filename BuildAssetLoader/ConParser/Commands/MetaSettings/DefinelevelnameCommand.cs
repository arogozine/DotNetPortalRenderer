namespace BuildAssetLoader.Con
{
    // definelevelname <episode> <levelnum> <mapname> <partime> <3dr> <levname>
    // partime/3dr are MM:SS-formatted clock strings (e.g. "01:45"), not integers.
    public sealed record DefinelevelnameCommand(int Episode, int LevelNum, string MapName, string ParTime, string DesignerTime, string LevelName)
        : Command(CommandList.definelevelname);
}

