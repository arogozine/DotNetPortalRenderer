// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Finds any sprite (actor or not, including sleeping actors) of the given tile number within a
    /// cylindrical search radius, storing its sprite id in <c>Gamevar</c> (or -1 if none is found).</summary>
    [Description("findnearsprite")]
    public sealed record FindNearSpriteCommand(
        string TileNumber,
        string Distance,
        string Gamevar)
        : BaseFindNearCommand(CommandList.FindNearSprite, TileNumber, Distance, Gamevar);
}
