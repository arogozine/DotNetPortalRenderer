// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Conditional returning true if the number of players is less than Value. A legacy command from
    /// Duke Nukem 3D v1.1, superseded by <c>ifmultiplayer</c> in later versions.</summary>
    [Description("ifplayersl")]
    public sealed record IfPlayersLCommand(
        string Value) : ConditionalStructure(CommandList.IfPlayersL);
}
