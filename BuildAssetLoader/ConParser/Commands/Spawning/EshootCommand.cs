// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Causes the current actor to fire the projectile of the given tile number, and sets gamevar RETURN
    /// to the new projectile's sprite id.</summary>
    [Description("eshoot")]
    public sealed record EShootCommand(
        string TileNumber) : BaseShootCommand(CommandList.EShoot, TileNumber);
}
