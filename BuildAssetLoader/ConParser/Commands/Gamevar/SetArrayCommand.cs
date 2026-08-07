// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Gamevar Operators =====
    // setvar/addvar/.../randvar -> VarOpCommand, setvarvar/.../randvarvar -> VarVarOpCommand (see Commands.cs, GamevarOperator, §3.5).

    /// <summary>Sets element <c>Index</c> of gamearray <c>Array</c> to <c>Value</c> (a gamevar or a constant). This
    /// is the only way to modify a single element of a gamearray without overwriting it entirely.</summary>
    [Description("setarray")]
    public sealed record SetArrayCommand(
        string Array,
        string Index,
        string Value) : Command(CommandList.SetArray);
}
