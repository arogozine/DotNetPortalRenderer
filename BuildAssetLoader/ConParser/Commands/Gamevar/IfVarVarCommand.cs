// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Represents the <c>ifvarvar&lt;cond&gt;</c> family of conditionals (<c>ifvarvare</c>,
    /// <c>ifvarvarn</c>, <c>ifvarvarg</c>, <c>ifvarvarl</c>, <c>ifvarvarand</c>, <c>ifvarvaror</c>,
    /// <c>ifvarvarxor</c>, <c>ifvarvareither</c>), which compare <c>Gamevar</c> against the gamevar
    /// <c>OtherGamevar</c> using <c>Condition</c> and branch into the following then/else structure. <c>Start</c>
    /// carries the exact originating CON token.</summary>
    [Description("ifvarvare")]
    public sealed record IfVarVarCommand(
        CommandList Start,
        GamevarCondition Condition,
        string Gamevar,
        string OtherGamevar) : ConditionalStructure(Start);
}
