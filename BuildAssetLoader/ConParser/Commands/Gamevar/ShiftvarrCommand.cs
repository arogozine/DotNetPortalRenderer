// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Shifts the value of <c>Gamevar</c> right by <c>Number</c> bits (each right shift by 1 halves the
    /// value). See also <c>shiftvarl</c>.</summary>
    [Description("shiftvarr")]
    public sealed record ShiftVarRCommand(
        string Gamevar,
        string Number) : Command(CommandList.ShiftVarR);
}
