// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // CommandList.Else is consumed into ConditionalStructure.ElseBody — no standalone record (see §5).

    // ===== Termination =====

    /// <summary>Stops execution of the code following it within the current block — commonly used to close a
    /// <c>case</c> in a <c>switch</c>, or to exit a state early. Inside a <c>while</c> loop, <c>break</c>
    /// transfers control back to the loop's condition check rather than exiting the loop (use <c>continue</c> or
    /// <c>exit</c> there instead, for clarity).</summary>
    [Description("break_")]
    public sealed record BreakCommand() : Command(CommandList.Break);
}
