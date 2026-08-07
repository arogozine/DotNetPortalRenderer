// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Selects the first available inventory item for the given player, also pruning empty items from
    /// the inventory in the process.</summary>
    [Description("checkavailinven")]
    public sealed record CheckAvailInvenCommand(
        string PlayerId) : Command(CommandList.CheckAvailInven);
}
