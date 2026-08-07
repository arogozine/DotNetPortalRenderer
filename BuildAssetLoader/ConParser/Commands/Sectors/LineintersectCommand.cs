// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Determines where a line from origin (<c>Ox</c>, <c>Oy</c>, <c>Oz</c>) to destination (<c>Dx</c>,
    /// <c>Dy</c>, <c>Dz</c>) intersects the vertical plane defined by points (<c>X1</c>, <c>Y1</c>) and (<c>X2</c>,
    /// <c>Y2</c>). The intersection point, if any, is written to <c>Intx</c>/<c>Inty</c>/<c>Intz</c>, with
    /// <c>Ret</c> set to 1 if an intersection exists and 0 otherwise.</summary>
    [Description("lineintersect")]
    public sealed record LineIntersectCommand(
        string Ox,
        string Oy,
        string Oz,
        string Dx,
        string Dy,
        string Dz,
        string X1,
        string Y1,
        string X2,
        string Y2,
        string Intx,
        string Inty,
        string Intz,
        string Ret)
        : Command(CommandList.LineIntersect);
}
