// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Global Settings - Object-Oriented =====

    /// <summary>Declares (outside any code block) a named animation for an actor, and at runtime switches the current
    /// actor to that action. <c>StartFrame</c> is the first frame relative to the actor's base tile, <c>Frames</c> is
    /// the frame count before looping, <c>ViewType</c> selects how many drawn angles the sprite has, <c>IncValue</c> is
    /// the per-tic frame advance (negative plays in reverse), and <c>Delay</c> sets the delay between frame
    /// advances.</summary>
    [Description("action")]
    public record ActionCommand(
        string Name,
        int? StartFrame,
        int? Frames,
        int? ViewType,
        int? IncValue,
        int? Delay)
         : Command(CommandList.Action);
}
