// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Defines a block of CON code to run when the named engine event fires. When multiple
    /// <c>onevent</c> blocks exist for the same event, each later block is prepended before the earlier ones, so
    /// they execute in reverse definition order (use <c>appendevent</c> instead for forward order).</summary>
    [Description("onevent")]
    public sealed record OnEventCommand(
        string EventName) : BaseEventCommand(CommandList.OnEvent, EventName);
}
