namespace BuildAssetLoader.Con
{
    // ifpinventory <item> <value> — item is an inventory index (GET_* define); value is often a define too.
    public sealed record IfpinventoryCommand(string Item, string Value) : ConditionalStructure(CommandList.ifpinventory);
}

