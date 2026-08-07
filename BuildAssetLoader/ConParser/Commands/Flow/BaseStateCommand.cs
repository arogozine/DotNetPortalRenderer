// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Global Settings - Subroutines (states) =====

    /// <summary>Shared base for the <c>state</c>/<c>defstate</c>/<c>prependstate</c>/<c>appendstate</c> block
    /// commands, which define a reusable, named chunk of CON code that can be invoked from actors, events, or other
    /// states. Terminated by <c>ends</c>.</summary>
    public record BaseStateCommand(
        CommandList Start,
        string Name) : Structure(Start, CommandList.Ends);
}
