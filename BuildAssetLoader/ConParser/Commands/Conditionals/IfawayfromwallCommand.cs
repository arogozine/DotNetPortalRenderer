// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Surroundings If =====

    /// <summary>Conditional returning true if the current actor touches no walls.</summary>
    [Description("ifawayfromwall")]
    public sealed record IfAwayFromWallCommand() : ConditionalStructure(CommandList.IfAwayFromWall);
}
