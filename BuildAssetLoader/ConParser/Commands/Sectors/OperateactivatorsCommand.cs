// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Triggers ACTIVATOR (tile 2) and ACTIVATORLOCKED (tile 4) sprites identified by <c>Lotag</c>, usable
    /// from both actor and event code. <c>PlayerId</c> selects which player receives the "LOCKED"/"UNLOCKED"
    /// messages when triggered; a value greater than the current MULTIMODE suppresses the messages entirely.</summary>
    [Description("operateactivators")]
    public sealed record OperateActivatorsCommand(
        string Lotag,
        string PlayerId) : Command(CommandList.OperateActivators);
}
