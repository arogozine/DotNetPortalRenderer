// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Spawns an actor of the given tile number at the current actor's position. This is the base form of
    /// the spawn family; unlike its e/q variants it does not set gamevar RETURN or use the decal deletion
    /// queue.</summary>
    [Description("spawn")]
    public sealed record SpawnCommand(
        string TileNumber) : BaseSpawnCommand(CommandList.Spawn, TileNumber);
}
