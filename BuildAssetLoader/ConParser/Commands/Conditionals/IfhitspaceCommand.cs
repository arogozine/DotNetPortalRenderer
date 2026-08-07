// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Checks whether the player has pressed the open button (space by default).</summary>
    [Description("ifhitspace")]
    public sealed record IfHitSpaceCommand() : ConditionalStructure(CommandList.IfHitSpace);
}
