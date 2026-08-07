namespace BuildAssetLoader.Con
{
    // ===== Cutscenes =====

    // startcutscene <cutscene path> — argument is a quote id holding the cutscene path.
    public sealed record StartcutsceneCommand(int QuoteId) : Command(CommandList.startcutscene);
}

