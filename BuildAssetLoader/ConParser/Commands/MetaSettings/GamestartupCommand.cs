// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Sets the game's startup parameters (visibility, damage, ammo caps, blast radii, etc.) as an
    /// ordered list of values: 26 parameters for v1.3D, 30 for v1.5 Atomic.</summary>
    [Description("gamestartup")]
    public sealed record GameStartupCommand(
        string[] Parameters) : Command(CommandList.GameStartup);
}
