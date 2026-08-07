// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Meta-Settings - If (ConditionalStructure, no args) =====

    /// <summary>Conditional structure that checks whether the relevant respawn mode is enabled: hard-coded
    /// monsters check RESPAWN_MONSTERS, hard-coded inventory items check RESPAWN_INVENTORY, and everything
    /// else checks RESPAWN_ITEMS.</summary>
    [Description("ifrespawn")]
    public sealed record IfRespawnCommand() : ConditionalStructure(CommandList.IfRespawn);
}
