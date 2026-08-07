// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Changes gameplay speed to <c>Rate</c> (default 120); can be used to produce "bullet time"
    /// effects.</summary>
    [Description("inittimer")]
    public sealed record InitTimerCommand(
        int Rate) : Command(CommandList.InitTimer);
}
