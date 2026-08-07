namespace BuildAssetLoader.Con
{
    public sealed record SetPlayerCommand(string? Id, string Member, string Value) : Command(CommandList.setplayer);
}

