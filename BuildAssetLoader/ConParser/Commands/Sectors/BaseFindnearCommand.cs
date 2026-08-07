// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Shared base for the findnearactor/findnearactorvar/findnearactor3d/findnearactor3dvar/findnearsprite/
    /// findnearspritevar/findnearsprite3d/findnearsprite3dvar family. Searches for a sprite of the given tile number
    /// within a cylinder (or sphere, for the "3d" variants) radius, storing its id in a gamevar (or -1 if none is
    /// found within range). This is a synthetic base class used by this codebase to share fields between those
    /// commands; it does not correspond to a single CON keyword itself.</summary>
    public abstract record BaseFindNearCommand(
        CommandList Start,
        string TileNumber,
        string Distance,
        string Gamevar) : Command(Start);
}
