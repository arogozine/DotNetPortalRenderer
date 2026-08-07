namespace BuildAssetLoader.Con
{
    // qsubstr <quote1> <quote2> <start> <length> — copies a substring of quote2 into quote1.
    public sealed record QsubstrCommand(int Quote1, int Quote2, int Start, int Length) : Command(CommandList.qsubstr);
}

