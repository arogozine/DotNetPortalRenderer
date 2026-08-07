namespace BuildAssetLoader.Con
{
    // ===== Meta-Settings - If (ConditionalStructure, no args) =====

    // ifrespawn { ... } [else { ... }]
    public sealed record IfrespawnCommand() : ConditionalStructure(CommandList.ifrespawn);
}

