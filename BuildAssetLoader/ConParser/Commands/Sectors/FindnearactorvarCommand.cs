// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Variable-tile-number form of <c>findnearactor</c>: finds a currently awake actor of the tile number
    /// held in <c>TileNumber</c> within a cylindrical search radius, storing its sprite id in <c>Gamevar</c> (or -1
    /// if none is found).</summary>
    [Description("findnearactorvar")]
    public sealed record FindNearActorVarCommand(
        string TileNumber,
        string Distance,
        string Gamevar)
        : BaseFindNearCommand(CommandList.FindNearActorVar, TileNumber, Distance, Gamevar);
}
