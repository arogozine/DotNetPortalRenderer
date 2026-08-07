// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Quotes =====

    /// <summary>Declaratively defines a quote (max 128 characters) for later use by <c>quote</c>, <c>qsprintf</c>,
    /// <c>qstrcat</c>, <c>qstrcpy</c>, <c>gametext</c> or <c>minitext</c>. Each quote number must be unique; quotes
    /// may be redefined mid-game with <c>redefinequote</c>.</summary>
    [Description("definequote")]
    public sealed record DefineQuoteCommand(
        int QuoteNumber,
        string QuoteText) : Command(CommandList.DefineQuote);
}
