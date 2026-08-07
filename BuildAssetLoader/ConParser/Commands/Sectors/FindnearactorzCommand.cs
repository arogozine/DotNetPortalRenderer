// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Finds a currently awake actor of the given tile number within a finite cylinder defined by separate
    /// horizontal and vertical distances, storing its sprite id in <c>Gamevar</c> (or -1 if none is found).</summary>
    [Description("findnearactorz")]
    public sealed record FindNearActorZCommand(
        string TileNumber,
        string XyDistance,
        string ZDistance,
        string Gamevar)
        : BaseFindNearZCommand(CommandList.FindNearActorZ, TileNumber, XyDistance, ZDistance, Gamevar);
}
