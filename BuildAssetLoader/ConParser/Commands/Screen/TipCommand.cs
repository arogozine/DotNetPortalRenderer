// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Disables the current/closest player's weapon and shows the "tip" graphic.</summary>
    [Description("tip")]
    public sealed record TipCommand() : Command(CommandList.Tip);
}
