// See: https://wiki.eduke32.com/wiki/Category:All_commands

namespace BuildAssetLoader.Con
{
    /// <summary>No CommandInfo/*.html page exists for this CommandList entry (likely a reserved/quick-access
    /// struct name in the wiki's own grouping rather than a documented statement). Modeled as an empty
    /// placeholder pending documentation; revisit if a concrete grammar is found.</summary>
    public sealed record ScreenCommand() : Command(CommandList.Screen);
}
