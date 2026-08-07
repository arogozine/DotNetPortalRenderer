// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Projects a ray from point (<c>X</c>, <c>Y</c>, <c>Z</c>) along direction vector (<c>Vx</c>,
    /// <c>Vy</c>, <c>Vz</c>) and determines where it intersects the vertical plane defined by points (<c>X1</c>,
    /// <c>Y1</c>) and (<c>X2</c>, <c>Y2</c>). The intersection point, if any, is written to
    /// <c>Intx</c>/<c>Inty</c>/<c>Intz</c>, with <c>Ret</c> set to 1 if an intersection exists and 0
    /// otherwise.</summary>
    [Description("rayintersect")]
    public sealed record RayIntersectCommand(
        string X,
        string Y,
        string Z,
        string Vx,
        string Vy,
        string Vz,
        string X1,
        string Y1,
        string X2,
        string Y2,
        string Intx,
        string Inty,
        string Intz,
        string Ret)
        : Command(CommandList.RayIntersect);
}
