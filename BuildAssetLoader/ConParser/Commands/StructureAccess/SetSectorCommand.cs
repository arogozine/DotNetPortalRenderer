namespace BuildAssetLoader.Con
{
    public sealed record SetSectorCommand(string? Id, string Member, string Value) : Command(CommandList.setsector);
}

