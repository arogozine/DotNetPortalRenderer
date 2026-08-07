// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Data Saving =====

    /// <summary>Restores the gamevar <c>VarName</c> from the user's configuration file, where it was previously
    /// persisted with <c>savegamevar</c>. If no saved value is found, the gamevar becomes 0. For per-actor/per-player
    /// gamevars, only the value at the current actor/player index is restored.</summary>
    [Description("readgamevar")]
    public sealed record ReadGameVarCommand(
        string VarName) : Command(CommandList.ReadGameVar);
}
