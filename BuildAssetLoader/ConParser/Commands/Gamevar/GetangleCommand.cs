// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Computes the angle formed by the displacement (<c>X</c>, <c>Y</c>) — equivalent to arctan2(Y, X) —
    /// and stores it in <c>Return</c>.</summary>
    [Description("getangle")]
    public sealed record GetAngleCommand(
        string Return,
        string X,
        string Y) : Command(CommandList.GetAngle);
}
