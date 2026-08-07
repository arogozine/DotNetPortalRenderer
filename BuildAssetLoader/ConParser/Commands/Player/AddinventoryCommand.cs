namespace BuildAssetLoader.Con
{
    // addinventory <index> <amount> — sets an inventory item's amount (or adjusts armor/gives an access card).
    // <amount> is frequently a define (e.g. STEROID_AMOUNT).
    public sealed record AddinventoryCommand(string Index, string Amount) : Command(CommandList.addinventory);
}

