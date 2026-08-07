namespace BuildAssetLoader.Con
{
    // ifcutscene <cutscene path> — argument is a quote id holding the cutscene path.
    public sealed record IfcutsceneCommand(int QuoteId) : ConditionalStructure(CommandList.ifcutscene);
}

