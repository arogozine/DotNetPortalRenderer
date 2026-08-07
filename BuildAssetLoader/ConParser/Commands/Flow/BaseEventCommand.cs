// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Global Settings - Procedural (events) =====

    /// <summary>Shared base for the <c>onevent</c>/<c>appendevent</c> block commands, which define a chunk of CON
    /// code to run when a named engine event fires. Terminated by <c>endevent</c>.</summary>
    public record BaseEventCommand(
        CommandList Start,
        string EventName) : Structure(Start, CommandList.EndEvent);
}
