// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Represents the <c>ifvar&lt;cond&gt;</c> family of conditionals (<c>ifvare</c>, <c>ifvarn</c>,
    /// <c>ifvarg</c>, <c>ifvarl</c>, <c>ifvarand</c>, <c>ifvaror</c>, <c>ifvarxor</c>, <c>ifvareither</c>), which
    /// compare <c>Gamevar</c> against the constant/define/label <c>Value</c> using <c>Condition</c> and branch into
    /// the following then/else structure. <c>Start</c> carries the exact originating CON token.</summary>
    [Description("ifvare")]
    public sealed record IfVarCommand(
        CommandList Start,
        GamevarCondition Condition,
        string Gamevar,
        string Value) : ConditionalStructure(Start);
}
