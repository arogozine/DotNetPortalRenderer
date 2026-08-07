namespace BuildAssetLoader.Con
{
    public sealed record StarttrackvarCommand(string Track) : BaseStarttrackCommand(CommandList.starttrackvar, Track);
}

