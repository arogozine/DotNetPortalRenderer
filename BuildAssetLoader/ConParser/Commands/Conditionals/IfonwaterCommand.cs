// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Conditional checking whether the current actor is in a sector with lotag 1 and is at a certain
    /// distance from the floor.</summary>
    [Description("ifonwater")]
    public sealed record IfOnWaterCommand() : ConditionalStructure(CommandList.IfOnWater);
}
