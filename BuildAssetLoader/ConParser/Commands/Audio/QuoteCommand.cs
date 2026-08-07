namespace BuildAssetLoader.Con
{
    // quote <quote number> — displays a quote centered at the top of the screen for ~2 seconds.
    public sealed record QuoteCommand(int QuoteNumber) : Command(CommandList.quote);
}

