// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Computes <c>Result</c> = (<c>Factor1</c> * <c>Factor2</c>) &gt;&gt; <c>RightShift</c>, using a
    /// 64-bit intermediate product so the multiplication cannot overflow the way chaining <c>mul</c>/<c>shiftvarr</c>
    /// on 32-bit gamevars would.</summary>
    [Description("mulscale")]
    public sealed record MulScaleCommand(
        string Result,
        string Factor1,
        string Factor2,
        string RightShift) : Command(CommandList.MulScale);
}
