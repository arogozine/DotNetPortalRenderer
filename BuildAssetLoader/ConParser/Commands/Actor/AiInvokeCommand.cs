namespace BuildAssetLoader.Con
{
    public sealed record AiInvokeCommand(string Name) : Command(CommandList.ai);
}

