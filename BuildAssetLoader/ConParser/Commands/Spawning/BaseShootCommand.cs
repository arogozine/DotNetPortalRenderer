// See: https://wiki.eduke32.com/wiki/Category:All_commands

namespace BuildAssetLoader.Con
{
    /// <summary>Shared base for the shoot/shootvar/eshoot/eshootvar family: causes the current actor to fire the
    /// projectile of the given tile number. Synthetic base class used to share logic between the CON commands in
    /// this codebase; not itself a CON keyword.</summary>
    public record BaseShootCommand(CommandList Start, string TileNumber) : Command(Start);
}
