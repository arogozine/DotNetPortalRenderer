namespace BuildAssetLoader.Con
{
    // getincangle <return> <angle1> <angle2> — signed shortest angular difference from angle1 to angle2.
    public sealed record GetincangleCommand(string Return, string Angle1, string Angle2) : Command(CommandList.getincangle);
}

