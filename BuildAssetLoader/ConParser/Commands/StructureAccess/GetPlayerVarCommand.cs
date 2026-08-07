namespace BuildAssetLoader.Con
{
    public sealed record GetPlayerVarCommand(string? Id, string Member, string Gamevar) : Command(CommandList.getplayervar);
}

