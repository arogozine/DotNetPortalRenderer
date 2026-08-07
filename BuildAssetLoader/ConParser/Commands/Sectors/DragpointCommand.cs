// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Moves a wall vertex to a new position, the same way it would be moved in the map editor. Often used
    /// together with <c>rotatepoint</c>, which computes the target coordinates for a rotation.</summary>
    [Description("dragpoint")]
    public sealed record DragPointCommand(
        string Wallnum,
        string X,
        string Y) : Command(CommandList.DragPoint);
}
