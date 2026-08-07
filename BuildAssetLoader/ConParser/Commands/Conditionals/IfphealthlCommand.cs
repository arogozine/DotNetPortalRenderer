// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Conditional returning true if the player's health is lower than Number. Similar to
    /// <c>ifstrength</c>.</summary>
    [Description("ifphealthl")]
    public sealed record IfPHealthLCommand(
        string Number) : ConditionalStructure(CommandList.IfPHealthL);
}
