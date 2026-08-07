// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Removes interpolation from a sector's movement, reverting it to the native 30Hz update rate. Useful
    /// for edge cases where CON-driven interpolation is undesirable, e.g. instant snap movements caused by
    /// explosions. See also <c>sectsetinterpolation</c>.</summary>
    [Description("sectclearinterpolation")]
    public sealed record SectClearInterpolationCommand(string Sectnum) : Command(CommandList.SectClearInterpolation);
}
