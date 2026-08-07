// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Conditional returning true if the current actor can see the player.</summary>
    [Description("ifcanseetarget")]
    public sealed record IfCanSeeTargetCommand() : ConditionalStructure(CommandList.IfCanSeeTarget);
}
