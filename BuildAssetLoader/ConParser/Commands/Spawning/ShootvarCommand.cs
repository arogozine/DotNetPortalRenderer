// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Like shoot, but reads the tile number to fire from a gamevar rather than a constant/define: causes
    /// the current actor to fire that projectile.</summary>
    [Description("shootvar")]
    public sealed record ShootVarCommand(
        string TileNumber) : BaseShootCommand(CommandList.ShootVar, TileNumber);
}
