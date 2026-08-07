namespace BuildAssetLoader.Con
{
    public sealed record IfactionCommand(string Action) : ConditionalStructure(CommandList.ifaction);
}

