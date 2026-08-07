// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Computes the signed shortest angular difference needed to turn from <c>Angle1</c> to <c>Angle2</c>
    /// and stores it in <c>Return</c>. The sign indicates whether the shortest turn is clockwise or
    /// counter-clockwise.</summary>
    [Description("getincangle")]
    public sealed record GetIncAngleCommand(
        string Return,
        string Angle1,
        string Angle2) : Command(CommandList.GetIncAngle);
}
