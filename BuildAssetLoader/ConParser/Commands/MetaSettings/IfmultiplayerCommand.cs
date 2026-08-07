namespace BuildAssetLoader.Con
{
    // ifmultiplayer { ... } [else { ... }]
    public sealed record IfmultiplayerCommand() : ConditionalStructure(CommandList.ifmultiplayer);
}

