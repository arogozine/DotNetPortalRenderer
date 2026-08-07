namespace BuildAssetLoader.Con
{
    public sealed record IfspawnedbyCommand(string Actor) : ConditionalStructure(CommandList.ifspawnedby);
}

