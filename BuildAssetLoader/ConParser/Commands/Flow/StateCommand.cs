namespace BuildAssetLoader.Con
{
    public sealed record StateCommand(string Name) : BaseStateCommand(CommandList.state, Name);
}

