// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Like espawn, but reads the tile number to spawn from a gamevar rather than a constant/define:
    /// spawns an actor at the current actor's position and sets gamevar RETURN to the new sprite's id.</summary>
    [Description("espawnvar")]
    public sealed record ESpawnVarCommand(
        string TileNumber) : BaseSpawnCommand(CommandList.ESpawnVar, TileNumber);
}
