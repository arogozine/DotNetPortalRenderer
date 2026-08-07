// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Exits the current state early without propagating further up the call chain, unlike
    /// <c>return</c>, which terminates the entire chain of states back to the originating event or actor
    /// code.</summary>
    [Description("terminate")]
    public sealed record TerminateCommand() : Command(CommandList.Terminate);
}
