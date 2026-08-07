// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Checks if the current actor is in a sector where the gap between ceiling and floor is below a
    /// hardcoded threshold. Note this is hardcoded to display quote #10 ("Squish!") when true, even if no actor
    /// has actually been killed.</summary>
    [Description("ifsquished")]
    public sealed record IfSquishedCommand() : ConditionalStructure(CommandList.IfSquished);
}
