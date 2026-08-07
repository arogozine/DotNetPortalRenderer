// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Stops player movement (except mouse look up/down) for the number of tics given by a gamevar, where
    /// each unit is 1/30 of a second. Caution: using this while the player is underwater can prevent them from
    /// surfacing.</summary>
    [Description("lockplayer")]
    public sealed record LockPlayerCommand(
        string Gamevar) : Command(CommandList.LockPlayer);
}
