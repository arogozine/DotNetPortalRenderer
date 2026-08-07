namespace BuildAssetLoader.Con
{
    // getarraysequence <gamearray> <gamevar 1> [...] <gamevar N> — up to 32 gamevars.
    public sealed record GetarraysequenceCommand(string Gamearray, string[] Gamevars) : Command(CommandList.getarraysequence);
}

