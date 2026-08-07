// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Jumps execution back to the start of the enclosing loop's condition check. Should only be used
    /// inside a loop; behaves the same as <c>break</c> when used there.</summary>
    [Description("continue_")]
    public sealed record ContinueCommand() : Command(CommandList.Continue);
}
