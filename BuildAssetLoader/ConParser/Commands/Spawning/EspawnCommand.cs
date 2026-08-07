// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Spawns an actor of the given tile number at the current actor's position and sets gamevar RETURN
    /// to the new sprite's id.</summary>
    [Description("espawn")]
    public sealed record ESpawnCommand(
        string TileNumber) : BaseSpawnCommand(CommandList.ESpawn, TileNumber);
}
