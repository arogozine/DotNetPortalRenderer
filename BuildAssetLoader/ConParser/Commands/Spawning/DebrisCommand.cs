// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Materials =====

    /// <summary>Spawns the hard-coded debris tile (SCRAP1 through SCRAP6) at the current actor; the scraps inherit
    /// the owner's palette.</summary>
    [Description("debris")]
    public sealed record DebrisCommand(
        string TileNum,
        string Amount) : Command(CommandList.Debris);
}
