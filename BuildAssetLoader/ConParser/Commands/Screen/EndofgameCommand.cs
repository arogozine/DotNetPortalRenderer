// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Triggers the end of the episode after <c>Number</c> 1/15-second time units (default 52). Renamed
    /// from <c>endoflevel</c> after Duke Nukem 3D v1.1 to better reflect its function.</summary>
    [Description("endofgame")]
    public sealed record EndOfGameCommand(
        int Number) : BaseEndOfGameCommand(CommandList.EndOfGame, Number);
}
