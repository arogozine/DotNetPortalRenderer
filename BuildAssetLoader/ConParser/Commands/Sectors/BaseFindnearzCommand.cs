// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Shared base for the findnearactorz/findnearactorzvar/findnearspritez/findnearspritezvar family.
    /// Searches for a sprite of the given tile number within a finite cylinder defined by separate horizontal
    /// (<c>XyDistance</c>) and vertical (<c>ZDistance</c>) ranges, storing its id in a gamevar (or -1 if none is
    /// found within range). This is a synthetic base class used by this codebase to share fields between those
    /// commands; it does not correspond to a single CON keyword itself.</summary>
    public abstract record BaseFindNearZCommand(
        CommandList Start,
        string TileNumber,
        string XyDistance,
        string ZDistance,
        string Gamevar) : Command(Start);
}
