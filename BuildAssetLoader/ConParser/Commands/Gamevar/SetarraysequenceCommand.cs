namespace BuildAssetLoader.Con
{
    // setarraysequence <gamearray> <gamevar 1> [...] <gamevar N> — resizes the array to match the gamevar count.
    public sealed record SetarraysequenceCommand(string Gamearray, string[] Gamevars) : Command(CommandList.setarraysequence);
}

