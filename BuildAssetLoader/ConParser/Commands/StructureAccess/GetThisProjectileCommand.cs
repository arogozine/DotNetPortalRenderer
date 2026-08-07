namespace BuildAssetLoader.Con
{
    public sealed record GetThisProjectileCommand(string? Id, string Member, string Gamevar) : Command(CommandList.getthisprojectile);
}

