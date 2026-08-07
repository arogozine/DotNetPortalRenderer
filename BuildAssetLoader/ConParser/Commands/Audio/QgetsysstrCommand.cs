// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Copies a system string (e.g. STR_MAPNAME, STR_PLAYERNAME, STR_VERSION) identified by
    /// <c>StrId</c> into quote <c>QuoteId</c>. Used to display things like the level name or player name.</summary>
    [Description("qgetsysstr")]
    public sealed record QGetSysStrCommand(
        int QuoteId,
        string StrId) : Command(CommandList.QGetSysStr);
}
