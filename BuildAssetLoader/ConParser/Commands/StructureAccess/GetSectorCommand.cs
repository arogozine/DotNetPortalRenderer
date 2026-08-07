namespace BuildAssetLoader.Con
{
    public sealed record GetSectorCommand(string? Id, string Member, string Gamevar) : Command(CommandList.getsector);
}

