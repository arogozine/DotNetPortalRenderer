// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // Value in [-1, 255]; -1 always takes else, >=255 always takes if. Commonly a define (e.g. SWEARFREQUENCY).

    /// <summary>Probabilistic conditional: a Value of 255 or greater always takes the if-branch (100% chance), a
    /// Value of -1 always takes the else-branch, and any other value takes the if-branch on (Value+1)/256 of
    /// evaluations on average. Must only be used in synchronized code; use <c>displayrand</c> for display
    /// code.</summary>
    [Description("ifrnd")]
    public sealed record IfRndCommand(
        string Value) : ConditionalStructure(CommandList.IfRnd);
}
