// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Move (declare/invoke) =====

    /// <summary>Declares a named move, placed outside actor/state code, giving an actor a horizontal and vertical
    /// velocity (negative values reverse/invert direction). Both velocities are commonly omitted in practice,
    /// producing a zero-velocity "named marker" move, defaulting to 0.</summary>
    [Description("move")]
    public sealed record MoveCommand(
        string Name,
        int? Horizontal,
        int? Vertical) : Command(CommandList.Move);
}
