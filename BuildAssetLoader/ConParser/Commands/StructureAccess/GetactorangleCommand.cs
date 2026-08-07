// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Single-Use Structure Access (deprecated shortcuts, no struct-index syntax) =====

    /// <summary>Deprecated command. Gets the current actor's angle into <c>Gamevar</c>. Superseded by struct
    /// access (e.g. <c>getactor[].ang</c>).</summary>
    [Description("getactorangle")]
    public sealed record GetActorAngleCommand(
        string Gamevar) : Command(CommandList.GetActorAngle);
}
