// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Spawns the given number of pieces of paper at the current actor, using the same movement type as
    /// the money command.</summary>
    [Description("paper")]
    public sealed record PaperCommand(
        string Value) : Command(CommandList.Paper);
}
