// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Causes the current actor to fire the projectile of the given tile number with an explicit
    /// z-velocity, and sets gamevar RETURN to the new projectile's sprite id.</summary>
    [Description("ezshoot")]
    public sealed record EZShootCommand(
        string Zvel,
        string TileNumber) : BaseZShootCommand(CommandList.EZShoot, Zvel, TileNumber);
}
