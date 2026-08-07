// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Adds quote <c>QuoteNumber</c> (as defined by <c>definequote</c>) to the four-line multiplayer
    /// kill/death and chat text buffer, scrolling off after about six seconds. Unlike <c>quote</c>, every
    /// invocation is logged to the console and log file.</summary>
    [Description("userquote")]
    public sealed record UserQuoteCommand(
        int QuoteNumber) : Command(CommandList.UserQuote);
}
