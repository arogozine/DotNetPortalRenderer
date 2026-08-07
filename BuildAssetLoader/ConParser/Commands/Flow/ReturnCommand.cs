// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Stops execution of the code following it, like <c>break</c>, but propagates along the entire call
    /// chain of states, terminating the innermost event or actor code that was entered. Use <c>terminate</c>
    /// instead to exit only the current state.</summary>
    [Description("return_")]
    public sealed record ReturnCommand() : Command(CommandList.Return);
}
