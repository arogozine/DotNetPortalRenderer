// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Conditional returning true if the current actor's actioncount is equal to the given number.</summary>
    [Description("ifactioncount")]
    public sealed record IfActionCountCommand(
        string Number) : ConditionalStructure(CommandList.IfActionCount);
}
