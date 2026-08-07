// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Variable-tile-number form of <c>findnearsprite</c>: finds any sprite (actor or not, including
    /// sleeping actors) of the tile number held in <c>TileNumber</c> within a cylindrical search radius, storing
    /// its sprite id in <c>Gamevar</c> (or -1 if none is found).</summary>
    [Description("findnearspritevar")]
    public sealed record FindNearSpriteVarCommand(
        string TileNumber,
        string Distance,
        string Gamevar)
        : BaseFindNearCommand(CommandList.FindNearSpriteVar, TileNumber, Distance, Gamevar);
}
