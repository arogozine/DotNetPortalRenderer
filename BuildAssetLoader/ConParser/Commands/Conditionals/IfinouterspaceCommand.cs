// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Conditional returning true if the current actor is in a sector with a parallaxed space texture
    /// that has palette 0. Similar to <c>ifinspace</c> but more specific.</summary>
    [Description("ifinouterspace")]
    public sealed record IfInOuterSpaceCommand() : ConditionalStructure(CommandList.IfInOuterSpace);
}
