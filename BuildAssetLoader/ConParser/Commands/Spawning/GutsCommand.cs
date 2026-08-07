// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Spawns the hard-coded gore tile (e.g. JIBS1 through JIBS6, HEADJIB, LEGJIB, ARMJIB, LIZMANHEAD1,
    /// LIZMANARM1, LIZMANLEG1, DUKETORSO, DUKELEG, DUKEGUN) at the current actor.</summary>
    [Description("guts")]
    public sealed record GutsCommand(
        string TileNum,
        string Amount) : Command(CommandList.Guts);
}
