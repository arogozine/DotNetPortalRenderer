namespace BuildAssetLoader.Con
{
    // getpname <QUOTE #> <gamevar holding PLAYER ID> — copies a player's name into a quote.
    public sealed record GetpnameCommand(int QuoteNumber, string PlayerIdVar) : Command(CommandList.getpname);
}

