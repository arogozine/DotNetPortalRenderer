// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Rotates point (<c>X</c>, <c>Y</c>) around pivot (<c>Xpivot</c>, <c>Ypivot</c>) clockwise by
    /// <c>Ang</c> (2048 units per revolution), writing the resulting coordinates into <c>XReturnVar</c>/
    /// <c>YReturnVar</c>. Commonly used together with <c>dragpoint</c> to rotate sectors: this command computes
    /// where a wall point should end up, and <c>dragpoint</c> moves it there.</summary>
    [Description("rotatepoint")]
    public sealed record RotatePointCommand(
        string Xpivot,
        string Ypivot,
        string X,
        string Y,
        string Ang,
        string XReturnVar,
        string YReturnVar)
        : Command(CommandList.RotatePoint);
}
