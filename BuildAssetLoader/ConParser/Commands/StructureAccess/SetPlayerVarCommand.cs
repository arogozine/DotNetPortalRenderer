namespace BuildAssetLoader.Con
{
    public sealed record SetPlayerVarCommand(string? Id, string Member, string Value) : Command(CommandList.setplayervar);
}

