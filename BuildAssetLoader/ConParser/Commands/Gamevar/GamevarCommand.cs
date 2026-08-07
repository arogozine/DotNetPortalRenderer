// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Declares a gamevar: a named signed 32-bit integer that can be read/written from actors, states and
    /// events. <c>Value</c> is the initial value (defaults to 0 if omitted) and <c>Flags</c> is an optional bitmask
    /// selecting the variable's scope/storage (e.g. global, per-player, per-actor, read-only) — value and flags may
    /// both be omitted entirely to declare a global variable initialized to 0.</summary>
    [Description("gamevar")]
    public sealed record GameVarCommand(
        string Name,
        int? Value,
        int? Flags) : Command(CommandList.GameVar);
}
