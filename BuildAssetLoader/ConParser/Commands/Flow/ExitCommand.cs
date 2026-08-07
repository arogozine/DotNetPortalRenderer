// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Breaks out of the enclosing loop entirely, the way <c>break</c> works in most other languages.
    /// Should only be used inside loops, and is preferred over <c>break</c> when the intent is to leave the loop
    /// early.</summary>
    [Description("exit")]
    public sealed record ExitCommand() : Command(CommandList.Exit);
}
