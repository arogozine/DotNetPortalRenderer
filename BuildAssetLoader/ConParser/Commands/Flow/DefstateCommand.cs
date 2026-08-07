namespace BuildAssetLoader.Con
{
    public sealed record DefstateCommand(string Name) : BaseStateCommand(CommandList.defstate, Name);
}

