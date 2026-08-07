// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Player Interaction =====

    /// <summary>Calculates the angle the current sprite must face to point at its last-known target position
    /// (htlastvx/htlastvy), storing the result in <c>ReturnVar</c>.</summary>
    [Description("getangletotarget")]
    public sealed record GetAngleToTargetCommand(string ReturnVar) : Command(CommandList.GetAngleToTarget);
}
