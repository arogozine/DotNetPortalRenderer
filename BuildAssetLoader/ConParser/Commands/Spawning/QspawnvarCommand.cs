// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Like qspawn, but reads the tile number to spawn from a gamevar rather than a constant/define:
    /// spawns an actor at the current actor's position and inserts the new sprite into the decal deletion
    /// queue.</summary>
    [Description("qspawnvar")]
    public sealed record QSpawnVarCommand(
        string TileNumber) : BaseSpawnCommand(CommandList.QSpawnVar, TileNumber);
}
