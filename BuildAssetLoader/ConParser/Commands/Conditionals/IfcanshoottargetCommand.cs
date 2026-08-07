// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Conditional returning true if the current actor can shoot the player.</summary>
    [Description("ifcanshoottarget")]
    public sealed record IfCanShootTargetCommand() : ConditionalStructure(CommandList.IfCanShootTarget);
}
