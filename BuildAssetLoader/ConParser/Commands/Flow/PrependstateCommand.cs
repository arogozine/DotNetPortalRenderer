namespace BuildAssetLoader.Con
{
    // Modifies an existing state by prepending code to its body (analog of onevent, for states).
    public sealed record PrependstateCommand(string Name) : BaseStateCommand(CommandList.prependstate, Name);
}

