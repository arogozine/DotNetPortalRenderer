namespace BuildAssetLoader.Con
{
    public sealed record GetActorVarCommand(string? Id, string Member, string Gamevar) : Command(CommandList.getactorvar);
}

