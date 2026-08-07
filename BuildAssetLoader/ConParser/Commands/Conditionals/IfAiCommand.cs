// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Conditional returning true if the current actor is currently performing the given ai function.</summary>
    [Description("ifai")]
    public sealed record IfAiCommand(
        string Ai) : ConditionalStructure(CommandList.IfAi);
}
