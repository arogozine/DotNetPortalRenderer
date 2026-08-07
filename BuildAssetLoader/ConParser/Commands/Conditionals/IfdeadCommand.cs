// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Checks whether the current actor is dead, i.e. its strength is 0 or less.</summary>
    [Description("ifdead")]
    public sealed record IfDeadCommand() : ConditionalStructure(CommandList.IfDead);
}
