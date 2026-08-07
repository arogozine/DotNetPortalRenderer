// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Persists the gamevar <c>VarName</c> to the user's configuration file, so it can be restored later
    /// with <c>readgamevar</c>. For per-actor/per-player gamevars, only the value at the current actor/player
    /// index is saved.</summary>
    [Description("savegamevar")]
    public sealed record SaveGameVarCommand(
        string VarName) : Command(CommandList.SaveGameVar);
}
