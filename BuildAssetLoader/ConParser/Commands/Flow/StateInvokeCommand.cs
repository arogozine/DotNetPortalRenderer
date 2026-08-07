// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // state <name> — invocation, runs a previously-defined state's code inline.

    /// <summary>Invokes a previously-declared state, running its code inline at this point in an actor, event, or
    /// another state.</summary>
    [Description("state")]
    public sealed record StateInvokeCommand(
        string Name) : Command(CommandList.State);
}
