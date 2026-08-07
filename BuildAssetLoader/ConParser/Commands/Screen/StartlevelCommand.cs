// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Schedules a map load for the given <c>Volume</c>/<c>Level</c>, bypassing the End of Level screen.
    /// The load is deferred rather than immediate, so a <c>loadmapstate</c> issued right after this does not
    /// restore the target map's state.</summary>
    [Description("startlevel")]
    public sealed record StartLevelCommand(
        string Volume,
        string Level) : Command(CommandList.StartLevel);
}
