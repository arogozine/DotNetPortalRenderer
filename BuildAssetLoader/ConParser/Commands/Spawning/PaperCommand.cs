namespace BuildAssetLoader.Con
{
    // paper <value> — spawns pieces of paper (uses money's movement type).
    public sealed record PaperCommand(string Value) : Command(CommandList.paper);
}

