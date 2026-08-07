// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Same as <c>resetplayer</c> (reloads the map in single player and clears the player's inventory,
    /// halting subsequent code similarly to <c>return</c>), with an extra bitfield of options; bit 1 skips asking
    /// the player whether to load their most recent save.</summary>
    [Description("resetplayerflags")]
    public sealed record ResetPlayerFlagsCommand(
        int Flags) : Command(CommandList.ResetPlayerFlags);
}
