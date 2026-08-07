namespace BuildAssetLoader.Con
{
    // qstrcat <quote1> <quote2> — appends quote2's text to quote1.
    public sealed record QstrcatCommand(int Quote1, int Quote2) : Command(CommandList.qstrcat);
}

