// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Checks if the current actor's current strength is less than or equal to Strength.</summary>
    [Description("ifstrength")]
    public sealed record IfStrengthCommand(
        string Strength) : ConditionalStructure(CommandList.IfStrength);
}
