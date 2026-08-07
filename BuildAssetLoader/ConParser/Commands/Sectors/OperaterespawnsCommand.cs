namespace BuildAssetLoader.Con
{
    // operaterespawns <lotag number>
    public sealed record OperaterespawnsCommand(string LotagNumber) : Command(CommandList.operaterespawns);
}

