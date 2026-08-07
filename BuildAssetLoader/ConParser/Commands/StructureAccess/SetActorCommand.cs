namespace BuildAssetLoader.Con
{
    public sealed record SetActorCommand(string? Id, string Member, string Value) : Command(CommandList.setactor);
}

