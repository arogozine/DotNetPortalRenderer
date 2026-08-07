namespace BuildAssetLoader.Con
{
    // qstrncat <quote1> <quote2> <num> — appends the first <num> characters of quote2 to quote1.
    public sealed record QstrncatCommand(int Quote1, int Quote2, int Num) : Command(CommandList.qstrncat);
}

