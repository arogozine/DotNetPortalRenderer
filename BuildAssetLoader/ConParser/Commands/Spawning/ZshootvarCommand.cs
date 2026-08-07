// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Like zshoot, but reads the tile number to fire from a gamevar rather than a constant/define: fires
    /// a projectile with an explicit z-velocity.</summary>
    [Description("zshootvar")]
    public sealed record ZShootVarCommand(
        string Zvel,
        string TileNumber) : BaseZShootCommand(CommandList.ZShootVar, Zvel, TileNumber);
}
