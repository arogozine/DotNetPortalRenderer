// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Simulates movement of an arbitrary object starting at (<c>X</c>, <c>Y</c>, <c>Z</c>) in sector
    /// <c>SectNum</c>, at velocity (<c>XVect</c>, <c>YVect</c>) scaled by 14 bits, keeping at least <c>WallDist</c>
    /// from walls, <c>FlorDist</c> from floors and <c>CeilDist</c> from ceilings; <c>ClipMask</c> selects which
    /// walls/sprites participate in collision. <c>Return</c>, <c>X</c>, <c>Y</c> and <c>SectNum</c> must be
    /// writeable gamevars and are updated with the resulting position and what (if anything) was touched.</summary>
    [Description("clipmove")]
    public sealed record ClipMoveCommand(
        string Return,
        string X,
        string Y,
        string Z,
        string SectNum,
        string XVect,
        string YVect,
        string WallDist,
        string FlorDist,
        string CeilDist,
        string ClipMask)
        : Command(CommandList.ClipMove);
}
