namespace BuildAssetLoader.Con
{
    // ifclient { ... } [else { ... }]
    public sealed record IfclientCommand() : ConditionalStructure(CommandList.ifclient);
}

