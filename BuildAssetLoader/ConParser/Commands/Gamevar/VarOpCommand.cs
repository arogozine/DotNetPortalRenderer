// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Gamevar operator / condition records (Phase 4/3, declared here alongside the enums per §3.5) =====

    /// <summary>Represents the <c>&lt;op&gt;var</c> family of binary operators (<c>setvar</c>, <c>addvar</c>,
    /// <c>subvar</c>, <c>mulvar</c>, <c>divvar</c>, <c>modvar</c>, <c>andvar</c>, <c>orvar</c>, <c>xorvar</c>,
    /// <c>randvar</c>), which apply <c>Operator</c> to <c>Gamevar</c> using the constant/define/label
    /// <c>Value</c> as the second operand and store the result back into <c>Gamevar</c>. <c>Start</c> carries the
    /// exact originating CON token.</summary>
    [Description("setvar")]
    public sealed record VarOpCommand(
        CommandList Start,
        GamevarOperator Operator,
        string Gamevar,
        string Value) : Command(Start);
}
