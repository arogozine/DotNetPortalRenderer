// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Copies the display name of a player (as entered in the player setup menu) into quote
    /// <c>QuoteNumber</c>. <c>PlayerIdVar</c> is a gamevar holding the player ID whose name should be copied.</summary>
    [Description("getpname")]
    public sealed record GetPNameCommand(
        int QuoteNumber,
        string PlayerIdVar) : Command(CommandList.GetPName);
}
