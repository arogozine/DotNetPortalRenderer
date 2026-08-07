// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Like eshoot, but reads the tile number to fire from a gamevar rather than a constant/define: fires
    /// a projectile from the current actor and sets gamevar RETURN to the new projectile's sprite id.</summary>
    [Description("eshootvar")]
    public sealed record EShootVarCommand(
        string TileNumber) : BaseShootCommand(CommandList.EShootVar, TileNumber);
}
