// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Displays quote <c>QuoteNumber</c> (as defined by <c>definequote</c>) centered at the top of the
    /// screen, fading out after about two seconds. Requires a context where the player is defined. Consecutive
    /// repeats of the same quote number are logged to the console only once; see <c>userquote</c> for full
    /// logging.</summary>
    [Description("quote")]
    public sealed record QuoteCommand(
        int QuoteNumber) : Command(CommandList.Quote);
}
