// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Debug =====

    /// <summary>Logs <c>Number</c> to the console/log and triggers a debugger breakpoint in non-release
    /// builds.</summary>
    [Description("debug")]
    public sealed record DebugCommand(
        string Number) : Command(CommandList.Debug);
}
