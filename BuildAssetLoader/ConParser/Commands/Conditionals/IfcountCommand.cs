// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // Number is frequently a define (e.g. SHRUNKDONECOUNT, THAWTIME) rather than a literal.

    /// <summary>Checks whether the current actor has counted at least Number tics (one tic is 1/30 of a
    /// second).</summary>
    [Description("ifcount")]
    public sealed record IfCountCommand(
        string Number) : ConditionalStructure(CommandList.IfCount);
}
