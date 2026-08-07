// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Variable-tile-number form of <c>findnearspritez</c>: finds any sprite (actor or not, including
    /// sleeping actors) of the tile number held in <c>TileNumber</c> within a finite cylinder defined by separate
    /// horizontal and vertical distances, storing its sprite id in <c>Gamevar</c> (or -1 if none is found).</summary>
    [Description("findnearspritezvar")]
    public sealed record FindNearSpriteZVarCommand(
        string TileNumber,
        string XyDistance,
        string ZDistance,
        string Gamevar)
        : BaseFindNearZCommand(CommandList.FindNearSpriteZVar, TileNumber, XyDistance, ZDistance, Gamevar);
}
