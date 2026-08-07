// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Checks if the current actor is in a sector with a parallaxed ceiling (sky).</summary>
    [Description("ifoutside")]
    public sealed record IfOutsideCommand() : ConditionalStructure(CommandList.IfOutside);
}
