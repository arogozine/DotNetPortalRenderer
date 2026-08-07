namespace BuildAssetLoader.Con
{
    public sealed record StarttrackCommand(string Track) : BaseStarttrackCommand(CommandList.starttrack, Track);
}

