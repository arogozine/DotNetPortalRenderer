// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Conditional returning true if the current actor is performing the specified action.</summary>
    [Description("ifaction")]
    public sealed record IfActionCommand(
        string Action) : ConditionalStructure(CommandList.IfAction);
}
