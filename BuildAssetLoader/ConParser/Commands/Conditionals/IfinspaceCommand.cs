// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Conditional returning true if the current actor is in a sector with a parallaxed space texture.
    /// Similar to <c>ifinouterspace</c> but less specific.</summary>
    [Description("ifinspace")]
    public sealed record IfInSpaceCommand() : ConditionalStructure(CommandList.IfInSpace);
}
