// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Game-Changing =====

    /// <summary>Activates the cheat identified by <c>CheatId</c> (see the CHEAT_* defines). Singleplayer only —
    /// throws an error otherwise — and also fires EVENT_ACTIVATECHEAT.</summary>
    [Description("activatecheat")]
    public sealed record ActivateCheatCommand(
        string CheatId) : Command(CommandList.ActivateCheat);
}
