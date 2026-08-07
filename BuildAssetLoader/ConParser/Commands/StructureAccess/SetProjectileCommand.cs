namespace BuildAssetLoader.Con
{
    public sealed record SetProjectileCommand(string? Id, string Member, string Value) : Command(CommandList.setprojectile);
}

