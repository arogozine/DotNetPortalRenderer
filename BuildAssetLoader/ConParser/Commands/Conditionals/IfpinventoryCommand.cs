// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>For inventory items and armor, takes the success branch if the nearest player's amount of Item is
    /// not equal to Value. For access cards (Item == GET_ACCESS), Value is ignored and the branch is taken
    /// whenever the nearest player owns the access card identified by the calling actor's palette.</summary>
    [Description("ifpinventory")]
    public sealed record IfPInventoryCommand(
        string Item,
        string Value) : ConditionalStructure(CommandList.IfPInventory);
}
