// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Shifts the value of <c>Gamevar</c> left by <c>Number</c> bits (each left shift by 1 doubles the
    /// value). See also <c>shiftvarr</c>.</summary>
    [Description("shiftvarl")]
    public sealed record ShiftVarLCommand(
        string Gamevar,
        string Number) : Command(CommandList.ShiftVarL);
}
