// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Sets the current actor's "sleep" counter (httimetosleep) to <c>Count</c>. The counter increments
    /// once per tic while the player is within 30000 units, and the actor goes back to sleep once it reaches
    /// 32767 and the player is far enough away.</summary>
    [Description("sleeptime")]
    public sealed record SleepTimeCommand(string Count) : Command(CommandList.SleepTime);
}
