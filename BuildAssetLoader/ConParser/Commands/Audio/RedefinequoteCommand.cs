// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Like <c>definequote</c>, but usable inside actors, events and states to redefine an existing
    /// quote's text mid-game.</summary>
    [Description("redefinequote")]
    public sealed record RedefineQuoteCommand(
        int QuoteNumber,
        string QuoteText) : Command(CommandList.RedefineQuote);
}
