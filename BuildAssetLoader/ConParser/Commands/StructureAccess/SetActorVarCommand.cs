namespace BuildAssetLoader.Con
{
    public sealed record SetActorVarCommand(string? Id, string Member, string Value) : Command(CommandList.setactorvar);
}

