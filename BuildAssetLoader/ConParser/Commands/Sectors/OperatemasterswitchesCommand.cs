namespace BuildAssetLoader.Con
{
    // operatemasterswitches <lotag number>
    public sealed record OperatemasterswitchesCommand(string LotagNumber) : Command(CommandList.operatemasterswitches);
}

