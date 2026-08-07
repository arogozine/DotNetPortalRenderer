// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Conditional returning true if the current actor is currently in an underwater sector.</summary>
    [Description("ifinwater")]
    public sealed record IfInWaterCommand() : ConditionalStructure(CommandList.IfInWater);
}
