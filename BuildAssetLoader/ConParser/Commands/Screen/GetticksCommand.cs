// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Time Access =====

    /// <summary>Assigns the number of milliseconds since the game started to <c>Gamevar</c>. Not synced across
    /// clients — intended for visuals/profiling only, not gameplay logic.</summary>
    [Description("getticks")]
    public sealed record GetTicksCommand(
        string Gamevar) : Command(CommandList.GetTicks);
}
