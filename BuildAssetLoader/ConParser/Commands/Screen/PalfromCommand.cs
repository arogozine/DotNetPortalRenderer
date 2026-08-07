// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Screen Manipulation =====

    /// <summary>Flashes the screen a given color by modifying the player's pals/pals_time. <c>Intensity</c>
    /// (0-255) is how opaque the flash is (0 transparent, 63 fully opaque, higher values prolong it), and
    /// <c>Red</c>/<c>Green</c>/<c>Blue</c> (0-63 each, default 0 if omitted) set the color.</summary>
    [Description("palfrom")]
    public sealed record PalFromCommand(
        int Intensity,
        int? Red,
        int? Green,
        int? Blue) : Command(CommandList.PalFrom);
}
