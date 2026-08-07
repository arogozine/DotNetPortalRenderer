// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Finds the nearest player, storing the distance to it in <c>Gamevar</c> and the found player's id in
    /// RETURN. When run from the APLAYER actor or a player event this returns the id of the calling player itself;
    /// use <c>findotherplayer</c> in that case to find a different player.</summary>
    [Description("findplayer")]
    public sealed record FindPlayerCommand(string Gamevar) : BaseFindPlayerCommand(CommandList.FindPlayer, Gamevar);
}
