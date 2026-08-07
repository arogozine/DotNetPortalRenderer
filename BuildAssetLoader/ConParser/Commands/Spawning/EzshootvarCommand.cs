// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Like ezshoot, but reads the tile number to fire from a gamevar rather than a constant/define:
    /// fires a projectile with an explicit z-velocity and sets gamevar RETURN to the new projectile's sprite
    /// id.</summary>
    [Description("ezshootvar")]
    public sealed record EZShootVarCommand(
        string Zvel,
        string TileNumber) : BaseZShootCommand(CommandList.EZShootVar, Zvel, TileNumber);
}
