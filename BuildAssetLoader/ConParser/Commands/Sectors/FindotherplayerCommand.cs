// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Finds the nearest player other than the one currently running this code, storing the distance to
    /// it in <c>Gamevar</c> and the found player's id in RETURN. Should be used instead of <c>findplayer</c> when
    /// running from the APLAYER actor or a player event, since <c>findplayer</c> would just return the calling
    /// player's own id.</summary>
    [Description("findotherplayer")]
    public sealed record FindOtherPlayerCommand(string Gamevar) : BaseFindPlayerCommand(CommandList.FindOtherPlayer, Gamevar);
}
