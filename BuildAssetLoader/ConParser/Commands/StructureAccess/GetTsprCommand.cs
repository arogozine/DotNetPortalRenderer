namespace BuildAssetLoader.Con
{
    public sealed record GetTsprCommand(string? Id, string Member, string Gamevar) : Command(CommandList.gettspr);
}

