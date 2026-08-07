// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Reloads the map (in single player) and clears the player's inventory. In single player, execution
    /// of subsequent code halts similarly to <c>return</c>.</summary>
    [Description("resetplayer")]
    public sealed record ResetPlayerCommand() : Command(CommandList.ResetPlayer);
}
