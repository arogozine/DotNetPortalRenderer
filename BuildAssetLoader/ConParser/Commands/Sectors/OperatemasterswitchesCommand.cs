// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Operates any MasterSwitch sprites sharing the given lotag.</summary>
    [Description("operatemasterswitches")]
    public sealed record OperateMasterSwitchesCommand(string LotagNumber) : Command(CommandList.OperateMasterSwitches);
}
