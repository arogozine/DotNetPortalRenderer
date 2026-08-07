// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Checks if the current actor is using the given move code.</summary>
    [Description("ifmove")]
    public sealed record IfMoveCommand(
        string Move) : ConditionalStructure(CommandList.IfMove);
}
