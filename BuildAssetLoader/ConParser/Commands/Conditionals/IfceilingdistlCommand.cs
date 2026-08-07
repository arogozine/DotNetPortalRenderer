// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Checks if the current actor is less than 256*Number z units away from its sector's ceiling (one
    /// unit corresponds to 16 x/y BUILD units). If the actor is "inside" the ceiling the underlying distance is
    /// negative, i.e. the condition is true.</summary>
    [Description("ifceilingdistl")]
    public sealed record IfCeilingDistLCommand(
        string Number) : ConditionalStructure(CommandList.IfCeilingDistL);
}
