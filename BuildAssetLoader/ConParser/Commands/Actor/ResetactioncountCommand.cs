// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Resets actioncount back to 0, restarting the current action from its first frame.</summary>
    [Description("resetactioncount")]
    public sealed record ResetActionCountCommand() : Command(CommandList.ResetActionCount);
}
