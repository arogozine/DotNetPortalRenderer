// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Causes the current actor to fire the projectile of the given tile number with an explicit
    /// z-velocity, useful for aiming vertically at something other than the player.</summary>
    [Description("zshoot")]
    public sealed record ZShootCommand(
        string Zvel,
        string TileNumber) : BaseZShootCommand(CommandList.ZShoot, Zvel, TileNumber);
}
