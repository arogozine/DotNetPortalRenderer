// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Global Settings - Subroutines helpers (ai declare/invoke) =====

    /// <summary>Declares (outside any actor or event code) a named "ai" routine combining an action, a move, and a
    /// set of hardcoded movement flags, so it can later be invoked by name from within actor code. <c>Name</c> is
    /// the routine's identifier, <c>Action</c> and <c>Move</c> are the action/move to use while it runs, and
    /// <c>MoveFlag</c> is the (possibly empty) sequence of hardcoded movement behavior flags.</summary>
    [Description("ai")]
    public sealed record AiCommand(
        string Name,
        string? Action,
        string? Move,
        string[]? MoveFlag)
        : Command(CommandList.Ai);
}
