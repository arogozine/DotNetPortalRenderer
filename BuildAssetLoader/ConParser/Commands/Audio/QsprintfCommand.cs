// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Copies the text of the quote at <c>SourceQuote</c> into <c>DestinationQuote</c>, substituting each
    /// <c>%d</c>/<c>%ld</c> in order with a gamevar from <c>Parameters</c> and each <c>%s</c> with a quote number
    /// from <c>Parameters</c>. Source and destination may be the same quote.</summary>
    [Description("qsprintf")]
    public sealed record QSprintfCommand(
        int DestinationQuote,
        int SourceQuote,
        string[] Parameters) : Command(CommandList.QSprintf);
}
