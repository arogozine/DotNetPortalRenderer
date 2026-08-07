// See: https://wiki.eduke32.com/wiki/Category:All_commands

namespace BuildAssetLoader.Con
{
    /// <summary>Shared base for the zshoot/zshootvar/ezshoot/ezshootvar family: fires a projectile of the given
    /// tile number with an explicit z-velocity, letting an actor aim vertically at something other than the
    /// player. Synthetic base class used to share logic between the CON commands in this codebase; not itself a
    /// CON keyword.</summary>
    public record BaseZShootCommand(CommandList Start, string Zvel, string TileNumber) : Command(Start);
}
