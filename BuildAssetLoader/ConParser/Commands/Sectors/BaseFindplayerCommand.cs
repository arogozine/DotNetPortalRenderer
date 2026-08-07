// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Shared base for <c>findplayer</c> and <c>findotherplayer</c>. Finds the nearest player, storing the
    /// distance to it in <c>Gamevar</c> and the found player's id in RETURN. This is a synthetic base class used by
    /// this codebase to share fields between those two commands; it does not correspond to a single CON keyword
    /// itself.</summary>
    public abstract record BaseFindPlayerCommand(
        CommandList Start,
        string Gamevar) : Command(Start);
}
