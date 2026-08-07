// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Checks whether the gap between the floor and ceiling of the sector the player is in is less than
    /// Number. Blockable sprites can also count as floors and ceilings for this check.</summary>
    [Description("ifgapzl")]
    public sealed record IfGapZLCommand(
        string Number) : ConditionalStructure(CommandList.IfGapZL);
}
