namespace BuildAssetLoader.Con
{
    public sealed record GetPlayerCommand(string? Id, string Member, string Gamevar) : Command(CommandList.getplayer);
}

