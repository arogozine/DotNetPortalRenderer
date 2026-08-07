// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Resets the current actor's count back to 0. Use <c>ifcount</c> to test count and <c>count</c> to set
    /// it manually.</summary>
    [Description("resetcount")]
    public sealed record ResetCountCommand() : Command(CommandList.ResetCount);
}
