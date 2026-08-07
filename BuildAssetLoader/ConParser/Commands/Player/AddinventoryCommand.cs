// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Adjusts an inventory-related value for the nearest player. For a regular inventory item, sets its
    /// current amount to the exact value given. For armor (index 1), adds the value to the current armor (a
    /// negative value subtracts, and armor can go negative). For access cards (index 6), the amount is ignored and
    /// the calling sprite's palette instead determines which keycard is given. Any other index is an error.</summary>
    [Description("addinventory")]
    public sealed record AddInventoryCommand(
        string Index,
        string Amount) : Command(CommandList.AddInventory);
}
