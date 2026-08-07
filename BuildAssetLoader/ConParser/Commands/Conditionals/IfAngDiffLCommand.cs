// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Player Interaction If =====

    /// <summary>Conditional returning true if the difference between the current actor's angle and the closest
    /// player's angle is less than the given number.</summary>
    [Description("ifangdiffl")]
    public sealed record IfAngDiffLCommand(
        string Number) : ConditionalStructure(CommandList.IfAngDiffL);
}
