// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Causes the current actor to fire the projectile of the given tile number.</summary>
    [Description("shoot")]
    public sealed record ShootCommand(
        string TileNumber) : BaseShootCommand(CommandList.Shoot, TileNumber);
}
