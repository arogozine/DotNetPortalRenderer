namespace BuildAssetLoader.Con
{
    // ifactorsound <sprite ID> <sound#>
    public sealed record IfactorsoundCommand(string SpriteId, string Sound) : ConditionalStructure(CommandList.ifactorsound);
}

