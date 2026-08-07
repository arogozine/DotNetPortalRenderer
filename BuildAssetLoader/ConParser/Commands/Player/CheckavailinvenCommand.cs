namespace BuildAssetLoader.Con
{
    // checkavailinven <playerID> — selects the first available inventory item, pruning empties.
    public sealed record CheckavailinvenCommand(string PlayerId) : Command(CommandList.checkavailinven);
}

