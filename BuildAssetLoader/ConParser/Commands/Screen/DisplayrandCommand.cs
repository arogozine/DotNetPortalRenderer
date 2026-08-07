// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Math (display) =====

    /// <summary>Generates a random number in [0, 32767] and assigns it to <c>Gamevar</c>. Sync-safe, so it may be
    /// used in unsynchronized (display) code unlike the regular random commands.</summary>
    [Description("displayrand")]
    public sealed record DisplayRandCommand(
        string Gamevar) : Command(CommandList.DisplayRand);
}
