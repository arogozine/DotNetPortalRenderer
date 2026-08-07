// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Restores the current actor's palette to whatever it was set to before the last <c>spritepal</c>
    /// call, using the value cached in the httempang member.</summary>
    [Description("getlastpal")]
    public sealed record GetLastPalCommand() : Command(CommandList.GetLastPal);
}
