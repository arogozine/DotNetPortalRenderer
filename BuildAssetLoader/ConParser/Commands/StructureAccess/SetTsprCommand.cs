namespace BuildAssetLoader.Con
{
    public sealed record SetTsprCommand(string? Id, string Member, string Value) : Command(CommandList.settspr);
}

