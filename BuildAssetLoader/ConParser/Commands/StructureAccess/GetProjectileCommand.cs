namespace BuildAssetLoader.Con
{
    public sealed record GetProjectileCommand(string? Id, string Member, string Gamevar) : Command(CommandList.getprojectile);
}

