namespace BuildAssetLoader.Con
{
    // ifserver { ... } [else { ... }]
    public sealed record IfserverCommand() : ConditionalStructure(CommandList.ifserver);
}

