namespace BuildAssetLoader.Con
{
    public sealed record GetWallCommand(string? Id, string Member, string Gamevar) : Command(CommandList.getwall);
}

