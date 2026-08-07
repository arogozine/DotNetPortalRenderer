// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Causes the current actor to open a nearby door.</summary>
    [Description("operate")]
    public sealed record OperateCommand() : Command(CommandList.Operate);
}
