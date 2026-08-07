// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Precaches the tile range Tile0-Tile1. Flag 0 precaches only if Tile0 is present in the map;
    /// flag 1 always precaches the range regardless of map contents.</summary>
    [Description("precache")]
    public sealed record PrecacheCommand(
        int Tile0,
        int Tile1,
        int Flag) : Command(CommandList.Precache);
}
