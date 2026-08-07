// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Actor If =====

    /// <summary>Conditional returning true if the current actor's picnum (tile number) matches the given tile
    /// number.</summary>
    [Description("ifactor")]
    public sealed record IfActorCommand(
        string TileNum) : ConditionalStructure(CommandList.IfActor);
}
