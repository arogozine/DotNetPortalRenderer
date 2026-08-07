// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Finds a currently awake actor of the given tile number within a spherical search radius, storing
    /// its sprite id in <c>Gamevar</c> (or -1 if none is found). The found actor need not be the closest one.</summary>
    [Description("findnearactor3d")]
    public sealed record FindNearActor3dCommand(
        string TileNumber,
        string Distance,
        string Gamevar)
        : BaseFindNearCommand(CommandList.FindNearActor3d, TileNumber, Distance, Gamevar);
}
