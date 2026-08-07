// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Player Actions =====

    /// <summary>Tilts the screen as if the player was struck, and resets vertical mouse aim. Semi-obsolete.</summary>
    [Description("wackplayer")]
    public sealed record WackPlayerCommand() : Command(CommandList.WackPlayer);
}
