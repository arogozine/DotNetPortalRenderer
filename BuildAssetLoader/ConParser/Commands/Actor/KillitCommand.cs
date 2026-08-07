// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Deletes the current actor from the map and halts further execution of its code, similar to
    /// <c>return</c>. Only sprites with a statnum such as STAT_ACTOR or STAT_MISC are actually removed; others
    /// merely disappear without being deleted.</summary>
    [Description("killit")]
    public sealed record KillItCommand() : Command(CommandList.KillIt);
}
