namespace BuildAssetLoader.Con
{
    // getangle <return> <x> <y> — arctan2(y, x).
    public sealed record GetangleCommand(string Return, string X, string Y) : Command(CommandList.getangle);
}

