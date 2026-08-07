// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Checks for the best available weapon for the given player (per wchoice priority) and switches to
    /// it if one is found.</summary>
    [Description("checkavailweapon")]
    public sealed record CheckAvailWeaponCommand(
        string PlayerId) : Command(CommandList.CheckAvailWeapon);
}
