// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Defines a block of CON code to run when the named engine event fires. When multiple
    /// <c>appendevent</c> blocks exist for the same event, each later block is appended after the earlier ones, so
    /// they execute in definition order; this is the recommended form for chaining multiple handlers of the same
    /// event across files.</summary>
    [Description("appendevent")]
    public sealed record AppendEventCommand(
        string EventName) : BaseEventCommand(CommandList.AppendEvent, EventName);
}
