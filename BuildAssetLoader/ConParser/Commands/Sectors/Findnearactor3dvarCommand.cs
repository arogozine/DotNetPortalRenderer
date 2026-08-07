// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Variable-tile-number form of <c>findnearactor3d</c>: finds a currently awake actor of the tile
    /// number held in <c>TileNumber</c> within a spherical search radius, storing its sprite id in <c>Gamevar</c>
    /// (or -1 if none is found).</summary>
    [Description("findnearactor3dvar")]
    public sealed record FindNearActor3dVarCommand(
        string TileNumber,
        string Distance,
        string Gamevar)
        : BaseFindNearCommand(CommandList.FindNearActor3dVar, TileNumber, Distance, Gamevar);
}
