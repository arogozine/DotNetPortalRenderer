namespace BuildAssetLoader.Con
{
    public sealed record IfaiCommand(string Ai) : ConditionalStructure(CommandList.ifai);
}

