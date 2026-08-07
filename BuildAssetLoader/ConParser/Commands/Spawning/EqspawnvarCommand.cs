// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Like eqspawn, but reads the tile number to spawn from a gamevar rather than a constant/define:
    /// spawns an actor at the current actor's position, sets gamevar RETURN to the new sprite's id, and inserts it
    /// into the decal deletion queue.</summary>
    [Description("eqspawnvar")]
    public sealed record EqSpawnVarCommand(
        string TileNumber) : BaseSpawnCommand(CommandList.EqSpawnVar, TileNumber);
}
