// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Finds any sprite (actor or not, including sleeping actors) of the given tile number within a finite
    /// cylinder defined by separate horizontal and vertical distances, storing its sprite id in <c>Gamevar</c> (or
    /// -1 if none is found).</summary>
    [Description("findnearspritez")]
    public sealed record FindNearSpriteZCommand(
        string TileNumber,
        string XyDistance,
        string ZDistance,
        string Gamevar)
        : BaseFindNearZCommand(CommandList.FindNearSpriteZ, TileNumber, XyDistance, ZDistance, Gamevar);
}
