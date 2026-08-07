namespace BuildAssetLoader.Con
{
    // getkeyname <quoteID> <funcID> <key> — copies a gamefunc's bound key name into a quote.
    public sealed record GetkeynameCommand(int QuoteId, string FuncId, string Key) : Command(CommandList.getkeyname);
}

