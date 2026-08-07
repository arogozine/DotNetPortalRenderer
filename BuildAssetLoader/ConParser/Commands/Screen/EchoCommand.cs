// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Prints the given quote to the console/log only, not to the screen.</summary>
    [Description("echo")]
    public sealed record EchoCommand(
        int QuoteNumber) : Command(CommandList.Echo);
}
