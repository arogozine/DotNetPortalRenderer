namespace BuildAssetLoader.Con
{
    // activate [<lotag>] — deprecated; same as operateactivators with a fixed player id of 0.
    public sealed record ActivateCommand(string? Lotag) : Command(CommandList.activate);
}

