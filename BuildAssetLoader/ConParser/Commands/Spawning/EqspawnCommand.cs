// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Spawns an actor of the given tile number at the current actor's position, sets gamevar RETURN to
    /// the new sprite's id, and inserts the new sprite into the decal deletion queue.</summary>
    [Description("eqspawn")]
    public sealed record EqSpawnCommand(
        string TileNumber) : BaseSpawnCommand(CommandList.EqSpawn, TileNumber);
}
