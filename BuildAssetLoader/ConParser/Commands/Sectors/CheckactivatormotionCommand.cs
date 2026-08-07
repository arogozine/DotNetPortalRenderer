// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Checks whether an activator with the given lotag is currently in motion, setting RETURN to 1 if so
    /// (and leaving it unchanged otherwise). Note this only detects activator motion, not delayed MasterSwitch
    /// motion.</summary>
    [Description("checkactivatormotion")]
    public sealed record CheckActivatorMotionCommand(string Lotag) : Command(CommandList.CheckActivatorMotion);
}
