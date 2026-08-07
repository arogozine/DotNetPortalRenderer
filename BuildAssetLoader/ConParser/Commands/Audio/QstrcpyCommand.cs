namespace BuildAssetLoader.Con
{
    // qstrcpy <quote1> <quote2> — copies quote2's text into quote1.
    public sealed record QstrcpyCommand(int Quote1, int Quote2) : Command(CommandList.qstrcpy);
}

