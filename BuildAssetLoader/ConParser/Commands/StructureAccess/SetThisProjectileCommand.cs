namespace BuildAssetLoader.Con
{
    public sealed record SetThisProjectileCommand(string? Id, string Member, string Value) : Command(CommandList.setthisprojectile);
}

