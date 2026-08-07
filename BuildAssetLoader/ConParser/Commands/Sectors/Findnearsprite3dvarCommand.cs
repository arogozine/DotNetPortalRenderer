// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Variable-tile-number form of <c>findnearsprite3d</c>: finds any sprite (actor or not, including
    /// sleeping actors) of the tile number held in <c>TileNumber</c> within a spherical search radius, storing its
    /// sprite id in <c>Gamevar</c> (or -1 if none is found).</summary>
    [Description("findnearsprite3dvar")]
    public sealed record FindNearSprite3dVarCommand(
        string TileNumber,
        string Distance,
        string Gamevar)
        : BaseFindNearCommand(CommandList.FindNearSprite3dVar, TileNumber, Distance, Gamevar);
}
