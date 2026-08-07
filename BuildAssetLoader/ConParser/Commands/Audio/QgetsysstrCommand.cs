namespace BuildAssetLoader.Con
{
    // qgetsysstr <quoteID> <strID> — copies a system string (STR_MAPNAME, STR_PLAYERNAME, ...) into a quote.
    public sealed record QgetsysstrCommand(int QuoteId, string StrId) : Command(CommandList.qgetsysstr);
}

