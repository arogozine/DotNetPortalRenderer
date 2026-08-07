// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Enables interpolation of a sector's movement, smoothing out CON-driven sector motion (e.g. via
    /// <c>dragpoint</c>) that would otherwise only update at the native 30Hz tic rate and look jittery. See also
    /// <c>sectclearinterpolation</c>.</summary>
    [Description("sectsetinterpolation")]
    public sealed record SectSetInterpolationCommand(string Sectnum) : Command(CommandList.SectSetInterpolation);
}
