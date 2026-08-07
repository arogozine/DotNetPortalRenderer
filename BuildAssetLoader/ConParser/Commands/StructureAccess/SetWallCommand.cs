namespace BuildAssetLoader.Con
{
    public sealed record SetWallCommand(string? Id, string Member, string Value) : Command(CommandList.setwall);
}

