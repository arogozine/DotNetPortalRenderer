namespace BuildAssetLoader.Con
{
    // ===== Actor If =====

    public sealed record IfactorCommand(string TileNum) : ConditionalStructure(CommandList.ifactor);
}

