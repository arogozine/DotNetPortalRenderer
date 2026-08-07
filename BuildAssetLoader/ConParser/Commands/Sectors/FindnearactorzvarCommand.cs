// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Variable-tile-number form of <c>findnearactorz</c>: finds a currently awake actor of the tile
    /// number held in <c>TileNumber</c> within a finite cylinder defined by separate horizontal and vertical
    /// distances, storing its sprite id in <c>Gamevar</c> (or -1 if none is found).</summary>
    [Description("findnearactorzvar")]
    public sealed record FindNearActorZVarCommand(
        string TileNumber,
        string XyDistance,
        string ZDistance,
        string Gamevar)
        : BaseFindNearZCommand(CommandList.FindNearActorZVar, TileNumber, XyDistance, ZDistance, Gamevar);
}
