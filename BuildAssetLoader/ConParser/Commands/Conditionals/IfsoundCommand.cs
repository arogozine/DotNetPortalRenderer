// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Audio/Cutscene If =====

    /// <summary>Conditional returning true if the given sound (a sound label define) is currently playing. Always
    /// returns false if the player has sound disabled in the menu.</summary>
    [Description("ifsound")]
    public sealed record IfSoundCommand(
        string Sound) : ConditionalStructure(CommandList.IfSound);
}
