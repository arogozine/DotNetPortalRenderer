namespace BuildAssetLoader.Con
{
    // redefinequote <quote number> <quote text> — like definequote, usable inside actors/events/states.
    public sealed record RedefinequoteCommand(int QuoteNumber, string QuoteText) : Command(CommandList.redefinequote);
}

