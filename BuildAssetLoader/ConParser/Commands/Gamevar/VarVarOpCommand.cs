// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Represents the <c>&lt;op&gt;varvar</c> family of binary operators (<c>setvarvar</c>,
    /// <c>addvarvar</c>, <c>subvarvar</c>, <c>mulvarvar</c>, <c>divvarvar</c>, <c>modvarvar</c>, <c>andvarvar</c>,
    /// <c>orvarvar</c>, <c>xorvarvar</c>, <c>randvarvar</c>), which apply <c>Operator</c> to <c>Gamevar</c> using
    /// the gamevar <c>OtherGamevar</c> as the second operand and store the result back into <c>Gamevar</c>.
    /// <c>Start</c> carries the exact originating CON token.</summary>
    [Description("setvarvar")]
    public sealed record VarVarOpCommand(
        CommandList Start,
        GamevarOperator Operator,
        string Gamevar,
        string OtherGamevar) : Command(Start);
}
