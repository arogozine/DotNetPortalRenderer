// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Spawns an actor of the given tile number at the current actor's position and inserts the new
    /// sprite into the decal deletion queue, like insertspriteq.</summary>
    [Description("qspawn")]
    public sealed record QSpawnCommand(
        string TileNumber) : BaseSpawnCommand(CommandList.QSpawn, TileNumber);
}
