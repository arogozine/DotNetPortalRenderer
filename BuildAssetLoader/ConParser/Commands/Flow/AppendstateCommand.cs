namespace BuildAssetLoader.Con
{
    // Modifies an existing state by appending code to its body (analog of appendevent, for states).
    public sealed record AppendstateCommand(string Name) : BaseStateCommand(CommandList.appendstate, Name);
}

