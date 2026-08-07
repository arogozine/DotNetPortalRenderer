// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Calculates the hypotenuse of a right triangle with legs <c>X</c> and <c>Y</c> (i.e. sqrt(x^2 + y^2))
    /// and stores the result in <c>ReturnVar</c>. Uses 64-bit intermediate arithmetic so the squared terms cannot
    /// overflow.</summary>
    [Description("calchypotenuse")]
    public sealed record CalcHypotenuseCommand(
        string ReturnVar,
        string X,
        string Y) : Command(CommandList.CalcHypotenuse);
}
