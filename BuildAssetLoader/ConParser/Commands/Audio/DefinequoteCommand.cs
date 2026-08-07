namespace BuildAssetLoader.Con
{
    // ===== Quotes =====

    // definequote <quote number> <quote text> — declarative, max 128 characters.
    public sealed record DefinequoteCommand(int QuoteNumber, string QuoteText) : Command(CommandList.definequote);
}

